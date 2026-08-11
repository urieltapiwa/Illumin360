namespace Illumin360.Matching;

/// <summary>A labelled training example: a numeric feature vector + a binary hire label (1 = hired).</summary>
/// <param name="Features">The feature vector (same length/order across all samples).</param>
/// <param name="Label">1 for a hire, 0 for a rejection.</param>
public sealed record RankSample(double[] Features, int Label);

/// <summary>
/// A trained pointwise learning-to-rank model (logistic regression). Standardises inputs with the means/
/// stds learned at training time, then applies the learned weights to produce a hire-probability, which
/// becomes a 0–100 ranking score. Deterministic and dependency-free — no ML framework.
/// </summary>
/// <param name="Weights">Per-feature weights (standardised space).</param>
/// <param name="Bias">Intercept.</param>
/// <param name="Mean">Per-feature training means (for standardisation).</param>
/// <param name="Std">Per-feature training std-devs (≥ 1e-9; constant features use 1).</param>
public sealed record RankModel(double[] Weights, double Bias, double[] Mean, double[] Std)
{
    /// <summary>Predicts the hire probability (0–1) for a feature vector.</summary>
    /// <param name="features">The feature vector.</param>
    /// <returns>Probability in [0, 1].</returns>
    public double Predict(double[] features)
    {
        ArgumentNullException.ThrowIfNull(features);
        var z = Bias;
        var n = Math.Min(features.Length, Weights.Length);
        for (var i = 0; i < n; i++)
        {
            var std = Std[i] <= 0 ? 1.0 : Std[i];
            z += Weights[i] * ((features[i] - Mean[i]) / std);
        }

        return 1.0 / (1.0 + Math.Exp(-z));
    }

    /// <summary>The predicted hire probability as a 0–100 ranking score.</summary>
    /// <param name="features">The feature vector.</param>
    /// <returns>A score in [0, 100].</returns>
    public int Score(double[] features) => (int)Math.Round(Math.Clamp(Predict(features), 0, 1) * 100);
}

/// <summary>Deterministic batch-gradient-descent trainer for a <see cref="RankModel"/> (logistic regression).</summary>
public static class LogisticRegressionTrainer
{
    /// <summary>Trains a model on the samples (features standardised internally).</summary>
    /// <param name="samples">Labelled training examples (all vectors same length).</param>
    /// <param name="iterations">Gradient-descent iterations.</param>
    /// <param name="learningRate">Step size.</param>
    /// <param name="l2">L2 regularisation strength.</param>
    /// <returns>The trained model (a zero model when there are no samples/features).</returns>
    public static RankModel Train(IReadOnlyList<RankSample> samples, int iterations = 500, double learningRate = 0.1, double l2 = 0.01)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (samples.Count == 0 || samples[0].Features.Length == 0)
        {
            return new RankModel([], 0, [], []);
        }

        var dim = samples[0].Features.Length;
        var (mean, std) = Standardisation(samples, dim);

        // Pre-standardise the design matrix once.
        var x = new double[samples.Count][];
        var y = new double[samples.Count];
        for (var r = 0; r < samples.Count; r++)
        {
            var row = new double[dim];
            for (var c = 0; c < dim; c++)
            {
                row[c] = (samples[r].Features[c] - mean[c]) / std[c];
            }

            x[r] = row;
            y[r] = samples[r].Label >= 1 ? 1.0 : 0.0;
        }

        var w = new double[dim];
        var b = 0.0;
        for (var iter = 0; iter < iterations; iter++)
        {
            var gradW = new double[dim];
            var gradB = 0.0;
            for (var r = 0; r < x.Length; r++)
            {
                var z = b;
                for (var c = 0; c < dim; c++)
                {
                    z += w[c] * x[r][c];
                }

                var pred = 1.0 / (1.0 + Math.Exp(-z));
                var err = pred - y[r];
                for (var c = 0; c < dim; c++)
                {
                    gradW[c] += err * x[r][c];
                }

                gradB += err;
            }

            for (var c = 0; c < dim; c++)
            {
                w[c] -= learningRate * ((gradW[c] / x.Length) + (l2 * w[c]));
            }

            b -= learningRate * (gradB / x.Length);
        }

        return new RankModel(w, b, mean, std);
    }

    private static (double[] Mean, double[] Std) Standardisation(IReadOnlyList<RankSample> samples, int dim)
    {
        var mean = new double[dim];
        var std = new double[dim];
        foreach (var s in samples)
        {
            for (var c = 0; c < dim; c++)
            {
                mean[c] += s.Features[c];
            }
        }

        for (var c = 0; c < dim; c++)
        {
            mean[c] /= samples.Count;
        }

        foreach (var s in samples)
        {
            for (var c = 0; c < dim; c++)
            {
                var d = s.Features[c] - mean[c];
                std[c] += d * d;
            }
        }

        for (var c = 0; c < dim; c++)
        {
            std[c] = Math.Sqrt(std[c] / samples.Count);
            if (std[c] < 1e-9)
            {
                std[c] = 1.0; // constant feature
            }
        }

        return (mean, std);
    }
}

/// <summary>The result of evaluating a trained ranker against a baseline on held-out data.</summary>
/// <param name="TrainCount">Training-set size.</param>
/// <param name="TestCount">Test-set size.</param>
/// <param name="ModelAuc">Model AUC on the test set.</param>
/// <param name="BaselineAuc">Baseline (current heuristic) AUC on the test set.</param>
/// <param name="Accuracy">Model accuracy at a 0.5 threshold.</param>
/// <param name="LogLoss">Model log-loss on the test set.</param>
public sealed record RankEvaluation(int TrainCount, int TestCount, double ModelAuc, double BaselineAuc, double Accuracy, double LogLoss)
{
    /// <summary>Whether the learned model out-ranks the baseline on the test set.</summary>
    public bool BetterThanBaseline => ModelAuc > BaselineAuc;
}

/// <summary>Trains + evaluates a ranker with a deterministic hold-out split (no RNG).</summary>
public static class RankEvaluator
{
    /// <summary>
    /// Splits samples deterministically (every 3rd row → test), trains on the rest, and scores both the
    /// learned model and a baseline on the test set by AUC (plus accuracy/log-loss for the model).
    /// </summary>
    /// <param name="samples">All labelled samples.</param>
    /// <param name="baselineScore">The current heuristic's score for a feature vector (any monotone scale).</param>
    /// <returns>The evaluation, or null when there isn't enough data / both classes to split.</returns>
    public static RankEvaluation? Evaluate(IReadOnlyList<RankSample> samples, Func<double[], double> baselineScore)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(baselineScore);

        var train = new List<RankSample>();
        var test = new List<RankSample>();
        for (var i = 0; i < samples.Count; i++)
        {
            (i % 3 == 2 ? test : train).Add(samples[i]);
        }

        if (train.Count < 2 || test.Count == 0 || test.All(s => s.Label == test[0].Label))
        {
            return null; // can't evaluate AUC without both classes in the test set
        }

        var model = LogisticRegressionTrainer.Train(train);
        var labels = test.Select(s => s.Label).ToList();
        var modelScores = test.Select(s => model.Predict(s.Features)).ToList();
        var baseScores = test.Select(s => baselineScore(s.Features)).ToList();

        var accuracy = test.Count == 0 ? 0 : (double)test.Select((s, i) => (modelScores[i] >= 0.5 ? 1 : 0) == s.Label ? 1 : 0).Sum() / test.Count;
        var logLoss = LogLoss(modelScores, labels);

        return new RankEvaluation(train.Count, test.Count, Math.Round(Auc(modelScores, labels), 3), Math.Round(Auc(baseScores, labels), 3), Math.Round(accuracy, 3), Math.Round(logLoss, 3));
    }

    // Rank-based AUC (probability a random positive outranks a random negative); 0.5 when a class is absent.
    private static double Auc(List<double> scores, List<int> labels)
    {
        double concordant = 0;
        long pairs = 0;
        for (var i = 0; i < labels.Count; i++)
        {
            if (labels[i] != 1)
            {
                continue;
            }

            for (var j = 0; j < labels.Count; j++)
            {
                if (labels[j] != 0)
                {
                    continue;
                }

                pairs++;
                if (scores[i] > scores[j])
                {
                    concordant += 1;
                }
                else if (Math.Abs(scores[i] - scores[j]) < 1e-12)
                {
                    concordant += 0.5;
                }
            }
        }

        return pairs == 0 ? 0.5 : concordant / pairs;
    }

    private static double LogLoss(List<double> probs, List<int> labels)
    {
        double sum = 0;
        for (var i = 0; i < probs.Count; i++)
        {
            var p = Math.Clamp(probs[i], 1e-12, 1 - 1e-12);
            sum += labels[i] == 1 ? -Math.Log(p) : -Math.Log(1 - p);
        }

        return probs.Count == 0 ? 0 : sum / probs.Count;
    }
}
