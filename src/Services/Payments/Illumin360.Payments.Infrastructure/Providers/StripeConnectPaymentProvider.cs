using System.Text.Json;
using Illumin360.Payments.Application.Abstractions;

namespace Illumin360.Payments.Infrastructure.Providers;

/// <summary>
/// Stripe Connect adapter (US/EU pilot — Stripe is not available for Namibian accounts). Maps cleanly onto the
/// <see cref="IPaymentProvider"/> port: a manual-capture PaymentIntent is the escrow "hold", capturing it is
/// the "release", and a refund reverses it — all keyed by the PaymentIntent id, so no destination/amount is
/// needed on release/refund. Form-encoded + bearer secret key + an Idempotency-Key header per Stripe's API.
///
/// NOTE: a real hold also needs a confirmed payment method (collected client-side); this creates the intent
/// and returns its id. Validate against Stripe's current API + a test-mode key before enabling. Off by default.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="options">Provider options (BaseUrl e.g. https://api.stripe.com, SecretKey sk_...).</param>
public sealed class StripeConnectPaymentProvider(HttpClient http, PaymentProviderOptions options) : IPaymentProvider
{
    private readonly HttpClient _http = http;
    private readonly PaymentProviderOptions _options = options;

    /// <inheritdoc />
    public Task<PaymentResult> CreateHoldAsync(string idempotencyKey, long amountMinor, string currency, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        var form = new[]
        {
            ("amount", amountMinor.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("currency", currency.ToLowerInvariant()),
            ("capture_method", "manual"),
        };
        return PostAsync("/v1/payment_intents", idempotencyKey, cancellationToken, form);
    }

    /// <inheritdoc />
    public async Task<PaymentResult> ReleaseAsync(ReleaseInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        // Separate charges + transfers (recipient known only at approval): capture the held PaymentIntent, then
        // transfer the amount to the talent's connected account. (If the recipient were fixed at hold time, the
        // destination-charge pattern — transfer_data[destination] on the intent — would avoid the second call.)
        var capture = await PostAsync($"/v1/payment_intents/{instruction.HoldReference}/capture", instruction.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        if (!capture.Success)
        {
            return capture;
        }

        var transfer = new[]
        {
            ("amount", instruction.AmountMinor.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("currency", instruction.Currency.ToLowerInvariant()),
            ("destination", instruction.DestinationAccount),
        };
        return await PostAsync("/v1/transfers", $"{instruction.IdempotencyKey}-transfer", cancellationToken, transfer).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<PaymentResult> RefundAsync(RefundInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);
        var form = new[]
        {
            ("payment_intent", instruction.HoldReference),
            ("amount", instruction.AmountMinor.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        };
        return PostAsync("/v1/refunds", instruction.IdempotencyKey, cancellationToken, form);
    }

    private async Task<PaymentResult> PostAsync(string path, string idempotencyKey, CancellationToken cancellationToken, params (string Key, string Value)[] form)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl!.TrimEnd('/')}{path}")
        {
            Content = new FormUrlEncodedContent(form.Select(f => new KeyValuePair<string, string>(f.Key, f.Value))),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.SecretKey);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return new PaymentResult(false, string.Empty, $"Stripe {(int)response.StatusCode}: {body}");
        }

        return new PaymentResult(true, ProviderJson.ReadString(body, "id") ?? idempotencyKey);
    }
}
