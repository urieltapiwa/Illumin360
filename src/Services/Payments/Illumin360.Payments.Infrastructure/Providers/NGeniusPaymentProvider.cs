using System.Net.Http.Headers;
using System.Net.Http.Json;
using Illumin360.Payments.Application.Abstractions;

namespace Illumin360.Payments.Infrastructure.Providers;

/// <summary>
/// Network International N-Genius Online adapter. Verified against docs.ngenius-payments.com (2026-08-12).
/// N-Genius is a <b>card-acquiring gateway with NO third-party payout API</b> — its money primitives are AUTH,
/// SALE, CAPTURE and REFUND (refunds go back to the original payer only). Captured funds settle to the
/// <i>merchant's</i> acquiring account; there is no documented way to disburse to a talent/seller. Therefore
/// this adapter can hold (AUTH) + refund-to-payer, but <b>cannot pay the talent</b> — <see cref="ReleaseAsync"/>
/// returns an explicit not-supported result and the payout must be done out-of-band. Not a marketplace-payout
/// provider. Off by default; D2-gated.
///
/// Base URLs: sandbox <c>https://api-gateway.sandbox.ngenius-payments.com</c>, prod
/// <c>https://api-gateway.ngenius-payments.com</c> (set via <see cref="PaymentProviderOptions.BaseUrl"/>).
/// Versioning is via vendor content types (identity: vnd.ni-identity.v1+json; payment: vnd.ni-payment.v2+json).
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="options">Provider options (BaseUrl, SecretKey = api key, Extra = outlet reference).</param>
public sealed class NGeniusPaymentProvider(HttpClient http, PaymentProviderOptions options) : IPaymentProvider
{
    private const string PaymentMediaType = "application/vnd.ni-payment.v2+json";
    private const string IdentityMediaType = "application/vnd.ni-identity.v1+json";

    private readonly HttpClient _http = http;
    private readonly PaymentProviderOptions _options = options;

    /// <inheritdoc />
    public async Task<PaymentResult> CreateHoldAsync(string idempotencyKey, long amountMinor, string currency, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        var token = await AccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (token is null)
        {
            return new PaymentResult(false, string.Empty, "N-Genius: could not obtain an access token.");
        }

        // Create an AUTH order (funds authorised = held). Amount is currencyCode + integer minor units.
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl!.TrimEnd('/')}/transactions/outlets/{_options.Extra}/orders")
        {
            Content = JsonContent.Create(new { action = "AUTH", amount = new { currencyCode = currency, value = amountMinor } }),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(PaymentMediaType);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(PaymentMediaType));

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode
            ? new PaymentResult(true, ProviderJson.ReadString(text, "reference") ?? idempotencyKey)
            : new PaymentResult(false, string.Empty, $"N-Genius {(int)response.StatusCode}: {text}");
    }

    /// <inheritdoc />
    public Task<PaymentResult> ReleaseAsync(ReleaseInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        // N-Genius has no third-party payout API: captured funds settle to the merchant, not the talent.
        return Task.FromResult(new PaymentResult(false, string.Empty, "N-Genius Online has no third-party payout API — capture settles to the merchant; disburse to the talent out-of-band."));
    }

    /// <inheritdoc />
    public Task<PaymentResult> RefundAsync(RefundInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        // Refund-to-payer IS supported, but the endpoint is HATEOAS-scoped to the payment + capture references
        // (…/orders/{ref}/payments/{ref}/captures/{ref}/refund) returned in the order/capture responses — which
        // this adapter does not persist. Extend the model to store those refs before wiring a real refund.
        return Task.FromResult(new PaymentResult(false, string.Empty, "N-Genius refund needs the payment/capture references from the order response — not persisted in this adapter; extend the model or refund via the portal."));
    }

    private async Task<string?> AccessTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl!.TrimEnd('/')}/identity/auth/access-token");
        request.Content = new StringContent(string.Empty);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(IdentityMediaType);

        // The portal Service-Account API key is used verbatim as the Basic credential (already encoded).
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
