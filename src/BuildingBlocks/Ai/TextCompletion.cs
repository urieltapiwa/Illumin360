using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Illumin360.Ai;

/// <summary>Which text-completion (LLM) backend to use.</summary>
public enum AiProviderKind
{
    /// <summary>No hosted model — callers use their deterministic local fallback (default).</summary>
    Disabled,

    /// <summary>A hosted, OpenAI-compatible chat-completions model (opt-in; sends prompts to an external service).</summary>
    Hosted,
}

/// <summary>
/// Configuration for the GenAI text-completion backend. <b>Default is <see cref="AiProviderKind.Disabled"/>
/// and <see cref="Enabled"/> = false</b> — no prompt ever leaves the platform unless a tenant explicitly turns
/// the hosted model on and supplies an endpoint. This is the flag the data-egress sign-off flips.
/// </summary>
public sealed record AiOptions
{
    /// <summary>The backend to use.</summary>
    public AiProviderKind Provider { get; init; } = AiProviderKind.Disabled;

    /// <summary>Per-tenant master switch for the hosted model.</summary>
    public bool Enabled { get; init; }

    /// <summary>The hosted chat-completions endpoint (OpenAI-compatible <c>/chat/completions</c> URL).</summary>
    public string? Endpoint { get; init; }

    /// <summary>The model name to request.</summary>
    public string Model { get; init; } = "gpt-4o-mini";

    /// <summary>Optional bearer API key for the endpoint.</summary>
    public string? ApiKey { get; init; }

    /// <summary>Whether the hosted model should actually be used (enabled + configured).</summary>
    public bool UseHosted => Provider == AiProviderKind.Hosted && Enabled && !string.IsNullOrWhiteSpace(Endpoint);
}

/// <summary>
/// Port for LLM text completion. Callers should check <see cref="Enabled"/> and fall back to their own
/// deterministic templating when it is false, so features work offline and only upgrade to real GenAI when a
/// hosted model is opted in.
/// </summary>
public interface ITextCompletionClient
{
    /// <summary>Whether a hosted model is configured + enabled.</summary>
    bool Enabled { get; }

    /// <summary>Completes the user prompt (with an optional system instruction).</summary>
    /// <param name="systemPrompt">System/role instruction (may be null).</param>
    /// <param name="userPrompt">The user prompt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The model's completion text.</returns>
    Task<string> CompleteAsync(string? systemPrompt, string userPrompt, CancellationToken cancellationToken);
}

/// <summary>The "off" client — <see cref="Enabled"/> is false and completion is never performed.</summary>
public sealed class DisabledTextCompletionClient : ITextCompletionClient
{
    /// <inheritdoc />
    public bool Enabled => false;

    /// <inheritdoc />
    public Task<string> CompleteAsync(string? systemPrompt, string userPrompt, CancellationToken cancellationToken)
        => throw new InvalidOperationException("The hosted AI model is disabled; callers must use their local fallback.");
}

/// <summary>
/// Calls a hosted, OpenAI-compatible <c>/chat/completions</c> endpoint. Only constructed when the tenant has
/// opted in via <see cref="AiOptions.UseHosted"/>. Failures surface to the caller (which can fall back).
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="options">The AI options (endpoint, model, key).</param>
public sealed class HostedTextCompletionClient(HttpClient http, AiOptions options) : ITextCompletionClient
{
    private readonly HttpClient _http = http;
    private readonly AiOptions _options = options;

    /// <inheritdoc />
    public bool Enabled => true;

    /// <inheritdoc />
    public async Task<string> CompleteAsync(string? systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new ChatMessage("system", systemPrompt));
        }

        messages.Add(new ChatMessage("user", userPrompt));

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = JsonContent.Create(new ChatRequest(_options.Model, messages)),
        };
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken).ConfigureAwait(false);
        return payload?.Choices is { Count: > 0 } choices ? choices[0].Message.Content.Trim() : string.Empty;
    }

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages);

    private sealed record ChatResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<ChatChoice> Choices);

    private sealed record ChatChoice(
        [property: JsonPropertyName("message")] ChatMessage Message);
}
