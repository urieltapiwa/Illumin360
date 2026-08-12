namespace Illumin360.Matching;

/// <summary>
/// Produces a fixed-length numeric embedding for a piece of text so semantic closeness can be measured by
/// cosine similarity. The default implementation (<see cref="HashingEmbeddingProvider"/>) is deterministic
/// and dependency-free; a hosted-model provider can be swapped in behind this interface without changing
/// the ranking code (see 03-architecture/semantic-matching-design.md).
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>The dimensionality of the vectors this provider returns.</summary>
    int Dimensions { get; }

    /// <summary>Embeds text into a unit-length vector (empty/blank text → a zero vector).</summary>
    /// <param name="text">The text to embed.</param>
    /// <returns>A vector of length <see cref="Dimensions"/>.</returns>
    float[] Embed(string? text);
}

/// <summary>
/// A deterministic, dependency-free embedding via the "feature hashing" trick: each token is hashed
/// (stable FNV-1a, not the process-randomised <c>string.GetHashCode</c>) into one of N buckets with a
/// signed contribution, then the vector is L2-normalised. Captures term overlap (and, with the same token
/// hashed the same way everywhere, gives a real cosine signal) with zero external calls — ideal for
/// offline/CI and as a graceful fallback. It is NOT true semantics; a hosted model behind
/// <see cref="IEmbeddingProvider"/> provides that when enabled.
/// </summary>
public sealed class HashingEmbeddingProvider(int dimensions = 256) : IEmbeddingProvider, IEmbeddingClient
{
    private static readonly char[] Separators =
        [' ', '\t', '\n', '\r', ',', '.', '/', '\\', '-', '_', '(', ')', '&', ':', ';', '|', '+'];

    private readonly int _dimensions = dimensions > 0 ? dimensions : 256;

    /// <inheritdoc />
    public int Dimensions => _dimensions;

    /// <inheritdoc />
    public Task<float[]> EmbedAsync(string? text, CancellationToken cancellationToken) => Task.FromResult(Embed(text));

    /// <inheritdoc />
    public float[] Embed(string? text)
    {
        var v = new float[_dimensions];
        if (string.IsNullOrWhiteSpace(text))
        {
            return v;
        }

        foreach (var token in text.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length < 2)
            {
                continue;
            }

            var h = Fnv1a(token.ToLowerInvariant());
            var index = (int)(h % (uint)_dimensions);
            var sign = ((h >> 31) & 1) == 0 ? 1f : -1f; // signed hashing reduces collision bias
            v[index] += sign;
        }

        return VectorMath.Normalize(v);
    }

    // Stable 32-bit FNV-1a — deterministic across processes/runs (unlike string.GetHashCode).
    private static uint Fnv1a(string s)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        var hash = offset;
        foreach (var c in s)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash;
    }
}
