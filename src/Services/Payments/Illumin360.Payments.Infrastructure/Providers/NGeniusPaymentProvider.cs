using System.Net.Http.Headers;
using System.Net.Http.Json;
using Illumin360.Payments.Application.Abstractions;

namespace Illumin360.Payments.Infrastructure.Providers;

/// <summary>
/// Network International N-Genius adapter. N-Genius is a two-step flow: fetch a short-lived access token
/// (Basic api-key), then create an order authorisation (hold), capture it (release), or refund it — scoped to
/// an outlet reference supplied via <see cref="PaymentProviderOptions.Extra"/>.
///
/// SCAFFOLD — N-Genius's order/capture API is card-flow specific and versioned; the auth handshake and the
/// order payload here follow the documented shape but MUST be validated against the N-Genius sandbox before
/// enabling. Off by default; D2-gated. (Same release-needs-amount port caveat as the other real adapters.)
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="options">Provider options (BaseUrl, SecretKey = api key, Extra = outlet reference).</param>
public sealed class NGeniusPaymentProvider(HttpClient http, PaymentProviderOptions options) : IPaymentProvider
{
    private readonly HttpClient _http = http;
    private readonly PaymentProviderOptions _options = options;

    /// <inheritdoc />
    public async Task<PaymentResult> CreateHoldAsync(string idempotencyKey, long amountMinor, string currency, CancellationToken cancellationToken)
    {
        var token = await AccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (token is null)
        {
            return new PaymentResult(false, string.Empty, "N-Genius: could not obtain an access token.");
        }

        // Create an AUTH order (funds authorised = held) for the outlet.
        var body = new { action = "AUTH", amount = new { currencyCode = currency, value = amountMinor }, merchantOrderReference = idempotencyKey };
        return await SendAsync(HttpMethod.Post, $"/transactions/outlets/{_options.Extra}/orders", token, body, idempotencyKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PaymentResult> ReleaseAsync(ReleaseInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);
        var token = await AccessTokenAsync(cancellationToken).ConfigureAwait(false);

        // Capture the authorised amount. N-Genius is card acquiring — paying the talent out to DestinationAccount
        // is a separate disbursement step (validate the payout rail against the sandbox).
        var body = new { amount = new { currencyCode = instruction.Currency, value = instruction.AmountMinor } };
        return token is null
            ? new PaymentResult(false, string.Empty, "N-Genius: could not obtain an access token.")
            : await SendAsync(HttpMethod.Post, $"/transactions/outlets/{_options.Extra}/orders/{instruction.HoldReference}/captures", token, body, instruction.IdempotencyKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PaymentResult> RefundAsync(RefundInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);
        var token = await AccessTokenAsync(cancellationToken).ConfigureAwait(false);
        var body = new { amount = new { currencyCode = instruction.Currency, value = instruction.AmountMinor } };
        return token is null
            ? new PaymentResult(false, string.Empty, "N-Genius: could not obtain an access token.")
            : await SendAsync(HttpMethod.Post, $"/transactions/outlets/{_options.Extra}/orders/{instruction.HoldReference}/refunds", token, body, instruction.IdempotencyKey, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> AccessTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl!.TrimEnd('/')}/identity/auth/access-token")
        {
            Content = JsonContent.Create(new { }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _options.SecretKey);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ProviderJson.ReadString(text, "access_token");
    }

    private async Task<PaymentResult> SendAsync(HttpMethod method, string path, string token, object body, string idempotencyKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, $"{_options.BaseUrl!.TrimEnd('/')}{path}")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode
            ? new PaymentResult(true, ProviderJson.ReadString(text, "reference") ?? ProviderJson.ReadString(text, "orderReference") ?? idempotencyKey)
            : new PaymentResult(false, string.Empty, $"N-Genius {(int)response.StatusCode}: {text}");
    }
}
