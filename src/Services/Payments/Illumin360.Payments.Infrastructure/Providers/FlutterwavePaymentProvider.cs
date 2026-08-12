using System.Net.Http.Json;
using Illumin360.Payments.Application.Abstractions;

namespace Illumin360.Payments.Infrastructure.Providers;

/// <summary>
/// Flutterwave adapter (recommended for a Namibia/SADC launch). JSON + bearer secret key against the v3 API.
/// Escrow is approximated by collecting into a platform balance/subaccount on hold, then paying out on release.
///
/// SCAFFOLD — validate against Flutterwave's current v3 docs + sandbox before enabling. Two known gaps to close
/// when going live (tracked in the design doc): (1) a real transfer/payout on release needs the talent's
/// destination account + the amount, which the current <see cref="IPaymentProvider"/> port does not carry —
/// the port needs a destination/amount extension; (2) refunds are by transaction id. Off by default; D2-gated.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="options">Provider options (BaseUrl e.g. https://api.flutterwave.com/v3, SecretKey FLWSECK-...).</param>
public sealed class FlutterwavePaymentProvider(HttpClient http, PaymentProviderOptions options) : IPaymentProvider
{
    private readonly HttpClient _http = http;
    private readonly PaymentProviderOptions _options = options;

    /// <inheritdoc />
    public Task<PaymentResult> CreateHoldAsync(string idempotencyKey, long amountMinor, string currency, CancellationToken cancellationToken)
        => PostAsync("/payments", idempotencyKey, new { tx_ref = idempotencyKey, amount = amountMinor / 100.0m, currency }, cancellationToken);

    /// <inheritdoc />
    public Task<PaymentResult> ReleaseAsync(ReleaseInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        // Transfer the released amount to the talent's subaccount/beneficiary (DestinationAccount).
        var body = new
        {
            account_bank = "subaccount",
            account_number = instruction.DestinationAccount,
            amount = instruction.AmountMinor / 100.0m,
            currency = instruction.Currency,
            reference = instruction.IdempotencyKey,
            meta = new { holdReference = instruction.HoldReference },
        };
        return PostAsync("/transfers", instruction.IdempotencyKey, body, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PaymentResult> RefundAsync(RefundInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);
        return PostAsync($"/transactions/{instruction.HoldReference}/refund", instruction.IdempotencyKey, new { amount = instruction.AmountMinor / 100.0m }, cancellationToken);
    }

    private async Task<PaymentResult> PostAsync(string path, string idempotencyKey, object body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl!.TrimEnd('/')}{path}")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.SecretKey);

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return new PaymentResult(false, string.Empty, $"Flutterwave {(int)response.StatusCode}: {text}");
        }

        // Flutterwave wraps the payload in { status, message, data: { id, ... } }.
        return new PaymentResult(true, ProviderJson.ReadString(text, "id", parent: "data") ?? idempotencyKey);
    }
}
