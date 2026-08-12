using System.Globalization;
using System.Net.Http.Json;
using Illumin360.Billing.Application.Abstractions;

namespace Illumin360.Billing.Infrastructure.Providers;

/// <summary>
/// Flutterwave v3 recurring adapter. Verified against developer.flutterwave.com (2026-08-12). Flutterwave has
/// native <b>Payment Plans</b> (a plan defines amount + interval) plus <b>tokenized charges</b> (after the first
/// card payment you receive a reusable <c>token</c> and re-charge via <c>/tokenized-charges</c>). We use the
/// tokenized-charge model so each cycle is an explicit server-initiated charge we control.
/// <para>
/// Money flow: <see cref="StartSubscriptionAsync"/> creates a hosted checkout (<c>/payments</c>) for the first
/// payment — it returns only a <c>data.link</c>, no id — and the reusable token arrives asynchronously via the
/// verify/webhook step (a follow-up); <see cref="ChargeAsync"/> re-charges that stored token.
/// </para>
/// <b>Currency caveat: Flutterwave has NO NAD corridor</b> (ZAR/USD are the usable options for the Namibian
/// context) — do not bill NAD through Flutterwave. Amounts are <b>major units</b> in v3. Off by default; D2-gated.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="options">Provider options (BaseUrl e.g. https://api.flutterwave.com/v3, SecretKey FLWSECK-...).</param>
public sealed class FlutterwaveBillingProvider(HttpClient http, BillingProviderOptions options) : IBillingProvider
{
    private readonly HttpClient _http = http;
    private readonly BillingProviderOptions _options = options;

    /// <inheritdoc />
    public async Task<BillingResult> StartSubscriptionAsync(string idempotencyKey, long amountMinor, string currency, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        // First payment via hosted checkout. tx_ref is our idempotency key; the reusable card token is captured
        // later from the verify/webhook payload (not synchronous here). We return the checkout link so the caller
        // can collect the mandate interactively.
        var body = new
        {
            tx_ref = idempotencyKey,
            amount = (amountMinor / 100.0m).ToString(CultureInfo.InvariantCulture),
            currency,
            redirect_url = "https://illumin360.example/billing/return",
            payment_options = "card",
            customer = new { email = "subscriber@illumin360.example" },
        };
        var (ok, text, status) = await PostAsync("/payments", body, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            return new BillingResult(false, string.Empty, Error: $"Flutterwave {status}: {text}");
        }

        var link = ProviderJson.ReadString(text, "link", parent: "data");
        return new BillingResult(true, idempotencyKey, CheckoutUrl: link);
    }

    /// <inheritdoc />
    public async Task<BillingResult> ChargeAsync(string idempotencyKey, string providerRef, long amountMinor, string currency, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerRef);

        // Re-charge the stored card token for one cycle.
        var body = new
        {
            token = providerRef,
            currency,
            amount = (amountMinor / 100.0m).ToString(CultureInfo.InvariantCulture),
            tx_ref = idempotencyKey,
            email = "subscriber@illumin360.example",
        };
        var (ok, text, status) = await PostAsync("/tokenized-charges", body, cancellationToken).ConfigureAwait(false);
        return ok
            ? new BillingResult(true, ProviderJson.ReadString(text, "flw_ref", parent: "data") ?? idempotencyKey)
            : new BillingResult(false, string.Empty, Error: $"Flutterwave {status}: {text}");
    }

    /// <inheritdoc />
    public async Task<BillingResult> CancelSubscriptionAsync(string providerRef, CancellationToken cancellationToken)
    {
        // A tokenized-charge mandate is cancelled by ceasing to charge it; there is no server token to revoke.
        // If a Payment-Plan subscription id is used instead, PUT /subscriptions/{id}/cancel deactivates it.
        var (ok, text, status) = await PutAsync($"/subscriptions/{providerRef}/cancel", cancellationToken).ConfigureAwait(false);
        return ok ? new BillingResult(true, providerRef) : new BillingResult(false, string.Empty, Error: $"Flutterwave {status}: {text}");
    }

    private async Task<(bool Ok, string Text, int Status)> PostAsync(string path, object body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl!.TrimEnd('/')}{path}")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.SecretKey);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return (response.IsSuccessStatusCode, text, (int)response.StatusCode);
    }

    private async Task<(bool Ok, string Text, int Status)> PutAsync(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"{_options.BaseUrl!.TrimEnd('/')}{path}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.SecretKey);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return (response.IsSuccessStatusCode, text, (int)response.StatusCode);
    }
}
