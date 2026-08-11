namespace Illumin360.Matching;

/// <summary>Pure vector helpers for embedding-based matching (unit-testable, no dependencies).</summary>
public static class VectorMath
{
    /// <summary>L2-normalises a vector to unit length (a zero vector is returned unchanged).</summary>
    /// <param name="v">The vector (mutated in place and returned).</param>
    /// <returns>The same array, normalised.</returns>
    public static float[] Normalize(float[] v)
    {
        ArgumentNullException.ThrowIfNull(v);
        double sumSq = 0;
        foreach (var x in v)
        {
            sumSq += (double)x * x;
        }

        if (sumSq <= 0)
        {
            return v;
        }

        var norm = (float)Math.Sqrt(sumSq);
        for (var i = 0; i < v.Length; i++)
        {
            v[i] /= norm;
        }

        return v;
    }

    /// <summary>Cosine similarity of two equal-length vectors (0 when either is a zero vector).</summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>Cosine similarity in [-1, 1] (0 if lengths differ or either is zero).</returns>
    public static double Cosine(float[] a, float[] b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.Length != b.Length || a.Length == 0)
        {
            return 0;
        }

        double dot = 0;
        double na = 0;
        double nb = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += (double)a[i] * b[i];
            na += (double)a[i] * a[i];
            nb += (double)b[i] * b[i];
        }

        if (na <= 0 || nb <= 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }
}
