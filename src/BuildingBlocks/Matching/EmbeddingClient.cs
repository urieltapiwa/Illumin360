using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Illumin360.Matching;

/// <summary>Which embedding backend to use.</summary>
public enum EmbeddingProviderKind
{
    /// <summary>Deterministic, dependency-free feature hashing (default; no external calls).</summary>
    Hashing,

    /// <summary>A hosted embedding model reached over HTTP (opt-in; sends text to an external service).</summary>
    Hosted,
}

/// <summary>
/// Configuration for the embedding backend. <b>Default is <see cref="EmbeddingProviderKind.Hashing"/> and
/// <see cref="Enabled"/> = false</b>, so no text ever leaves the platform unless a tenant explicitly turns
/// the hosted provider on (and supplies an endpoint) — honouring the data-egress governance gate.
/// </summary>
public sealed record EmbeddingOptions
{
    /// <summary>The backend to use.</summary>
    public EmbeddingProviderKind Provider { get; init; } = EmbeddingProviderKind.Hashing;

    /// <summary>
    /// Master per-tenant switch for the hosted provider. Even with <see cref="Provider"/> = Hosted, the hosted
    /// client is only used when this is true AND an <see cref="Endpoint"/> is set — otherwise it falls back to
    /// hashing. This is the flag the data-egress sign-off flips.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>The hosted embeddings endpoint (OpenAI-compatible <c>/embeddings</c> URL).</summary>
    public string? Endpoint { get; init; }

    /// <summary>The model name to request.</summary>
    public string Model { get; init; } = "text-embedding-3-small";

    /// <summary>Optional bearer API key for the endpoint.</summary>
    public string? ApiKey { get; init; }

    /// <summary>Vector dimensionality (must match the chosen model / the hashing fallback).</summary>
    public int Dimensions { get; init; } = 256;

    /// <summary>Whether the hosted provider should actually be used (enabled + configured).</summary>
    public bool UseHosted => Provider == EmbeddingProviderKind.Hosted && Enabled && !string.IsNullOrWhiteSpace(Endpoint);
}

/// <summary>
/// Async embedding port. Real/hosted models are network I/O, so this is the async counterpart of the
/// synchronous <see cref="IEmbeddingProvider"/>. The hashing provider implements both; the hosted client
/// implements this one. Ranking code depends on this port, so the backend can be swapped by config alone.
/// </summary>
public interface IEmbeddingClient
{
    /// <summary>The dimensionality of the vectors this client returns.</summary>
    int Dimensions { get; }

    /// <summary>Embeds text into a unit-length vector (blank text → a zero vector, no call).</summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A vector of length <see cref="Dimensions"/>.</returns>
    Task<float[]> EmbedAsync(string? text, CancellationToken cancellationToken);
}

/// <summary>
/// Calls a hosted, OpenAI-compatible <c>/embeddings</c> endpoint. <b>Only constructed/used when the tenant
/// has opted in via <see cref="EmbeddingOptions.UseHosted"/></b>. Blank text short-circuits to a zero vector
/// with no HTTP call; the returned vector is L2-normalised so cosine similarity behaves. Failures surface as
/// exceptions to the caller (which can fall back).
/// </summary>
/// <param name="http">The HTTP client (typically a typed/named client with the base address set).</param>
/// <param name="options">The embedding options (endpoint, model, key, dimensions).</param>
public sealed class HostedEmbeddingClient(HttpClient http, EmbeddingOptions options) : IEmbeddingClient
{
    private readonly HttpClient _http = http;
    private readonly EmbeddingOptions _options = options;

    /// <inheritdoc />
    public int Dimensions => _options.Dimensions;

    /// <inheritdoc />
    public async Task<float[]> EmbedAsync(string? text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new float[_options.Dimensions];
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = JsonContent.Create(new EmbeddingRequest(_options.Model, text)),
        };
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken).ConfigureAwait(false);
        var vector = payload?.Data is { Count: > 0 } data ? data[0].Embedding : null;
        return vector is { Length: > 0 } ? VectorMath.Normalize(vector) : new float[_options.Dimensions];
    }

    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input);

    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("data")] IReadOnlyList<EmbeddingDatum> Data);

    private sealed record EmbeddingDatum(
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
