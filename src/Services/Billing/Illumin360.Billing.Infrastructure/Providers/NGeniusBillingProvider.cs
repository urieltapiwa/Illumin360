using System.Net.Http.Headers;
using System.Net.Http.Json;
using Illumin360.Billing.Application.Abstractions;

namespace Illumin360.Billing.Infrastructure.Providers;

/// <summary>
/// Network International N-Genius Online recurring adapter. Verified against docs.ngenius-payments.com
/// (2026-08-12). N-Genius supports <b>merchant-initiated / saved-card</b> payments: the first order is created
/// with <c>savedCard.recapture</c> so the card is vaulted, and later cycles re-use the vaulted card via a
/// merchant-initiated order. OAuth: a Service-Account API key is exchanged for a bearer access token.
/// <para>
/// <b>Currency caveat: N-Genius is AED-centric</b> (Network International is a UAE acquirer) — no documented
/// NAD/ZAR rail — so it fits a UAE/MEA subscriber base, not Namibian NAD billing. Amount is currencyCode +
/// integer minor units. Off by default; D2-gated.
/// </para>
/// Base URLs: sandbox <c>https://api-gateway.sandbox.ngenius-payments.com</c>, prod
/// <c>https://api-gateway.ngenius-payments.com</c>.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="options">Provider options (BaseUrl, SecretKey = api key, Extra = outlet reference).</param>
public sealed class NGeniusBillingProvider(HttpClient http, BillingProviderOptions options) : IBillingProvider
{
    private const string PaymentMediaType = "application/vnd.ni-payment.v2+json";
    private const string IdentityMediaType = "application/vnd.ni-identity.v1+json";

    private readonly HttpClient _http = http;
    private readonly BillingProviderOptions _options = options;

    /// <inheritdoc />
    public async Task<BillingResult> StartSubscriptionAsync(string idempotencyKey, long amountMinor, string currency, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        var token = await AccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (token is null)
        {
            return new BillingResult(false, string.Empty, Error: "N-Genius: could not obtain an access token.");
        }

        // First order vaults the card (savedCard.recapture=true) for later merchant-initiated cycles.
        var body = new
        {
            action = "SALE",
            amount = new { currencyCode = currency, value = amountMinor },
            merchantOrderReference = idempotencyKey,
            savedCard = new { recapture = true },
        };
        var (ok, text, status) = await PostOrderAsync(token, body, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            return new BillingResult(false, string.Empty, Error: $"N-Genius {status}: {text}");
        }

        // The hosted payment-page link (for the first, card-capture payment) and the reference are in the response.
        var reference = ProviderJson.ReadString(text, "reference") ?? idempotencyKey;
        return new BillingResult(true, reference, CheckoutUrl: ProviderJson.ReadString(text, "href", parent: "_links"));
    }

    /// <inheritdoc />
    public async Task<BillingResult> ChargeAsync(string idempotencyKey, string providerRef, long amountMinor, string currency, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerRef);
        var token = await AccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (token is null)
        {
            return new BillingResult(false, string.Empty, Error: "N-Genius: could not obtain an access token.");
        }

        // Merchant-initiated cycle re-uses the vaulted card referenced by the first order.
        var body = new
        {
            action = "SALE",
            amount = new { currencyCode = currency, value = amountMinor },
            merchantOrderReference = idempotencyKey,
            savedCard = new { cardToken = providerRef },
            merchantDefinedData = new { subsequentType = "recurring" },
        };
        var (ok, text, status) = await PostOrderAsync(token, body, cancellationToken).ConfigureAwait(false);
        return ok
            ? new BillingResult(true, ProviderJson.ReadString(text, "reference") ?? idempotencyKey)
            : new BillingResult(false, string.Empty, Error: $"N-Genius {status}: {text}");
    }

    /// <inheritdoc />
    public Task<BillingResult> CancelSubscriptionAsync(string providerRef, CancellationToken cancellationToken)
    {
        // N-Genius has no server-side subscription object to cancel — a merchant-initiated mandate is ended by
        // ceasing to charge the vaulted card. Nothing to revoke remotely.
        return Task.FromResult(new BillingResult(true, providerRef));
    }

    private async Task<(bool Ok, string Text, int Status)> PostOrderAsync(string token, object body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl!.TrimEnd('/')}/transactions/outlets/{_options.Extra}/orders")
        {
            Content = JsonContent.Create(body),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(PaymentMediaType);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(PaymentMediaType));

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return (response.IsSuccessStatusCode, text, (int)response.StatusCode);
    }

    private async Task<string?> AccessTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl!.TrimEnd('/')}/identity/auth/access-token");
        request.Content = new StringContent(string.Empty);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(IdentityMediaType);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _options.SecretKey);

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ProviderJson.ReadString(text, "access_token");
    }
}
