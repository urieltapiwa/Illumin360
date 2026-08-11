using FluentAssertions;
using Illumin360.Matching;
using Xunit;

namespace Illumin360.Matching.UnitTests;

public class LearningToRankTests
{
    // A linearly-separable set: feature 0 drives the label.
    private static List<RankSample> SeparableSamples(int n)
    {
        var list = new List<RankSample>();
        for (var i = 0; i < n; i++)
        {
            var high = i % 2 == 0;
            var f0 = high ? 0.9 - (i * 0.001) : 0.1 + (i * 0.001);
            list.Add(new RankSample([f0, high ? 1.0 : 0.0], high ? 1 : 0));
        }

        return list;
    }

    [Fact]
    public void Trains_weights_that_separate_and_is_deterministic()
    {
        var samples = SeparableSamples(40);
        var m1 = LogisticRegressionTrainer.Train(samples);
        var m2 = LogisticRegressionTrainer.Train(samples);

        m1.Weights.Should().Equal(m2.Weights); // deterministic (no RNG)
        m1.Predict([0.95, 1.0]).Should().BeGreaterThan(m1.Predict([0.05, 0.0]));
        m1.Score([0.95, 1.0]).Should().BeInRange(0, 100);
    }

    [Fact]
    public void Evaluate_reports_model_beating_baseline_on_informative_features()
    {
        // Baseline uses feature 0 only; give the model an extra strongly-predictive feature 1
        // while feature 0 is pure noise, so the learned model should out-AUC the baseline.
        var samples = new List<RankSample>();
        for (var i = 0; i < 60; i++)
        {
            var hire = i % 2 == 0;
            var noise = (i % 5) / 5.0;                 // feature 0: uninformative
            var signal = hire ? 0.9 : 0.1;             // feature 1: strong signal
            samples.Add(new RankSample([noise, signal], hire ? 1 : 0));
        }

        var eval = RankEvaluator.Evaluate(samples, f => f[0]); // baseline = noisy feature 0

        eval.Should().NotBeNull();
        eval!.ModelAuc.Should().BeGreaterThan(eval.BaselineAuc);
        eval.BetterThanBaseline.Should().BeTrue();
        eval.ModelAuc.Should().BeInRange(0, 1);
    }

    [Fact]
    public void Evaluate_returns_null_without_both_classes_in_test()
    {
        var allHired = Enumerable.Range(0, 30).Select(i => new RankSample([0.5, 0.5], 1)).ToList();
        RankEvaluator.Evaluate(allHired, f => f[0]).Should().BeNull();
    }
}
