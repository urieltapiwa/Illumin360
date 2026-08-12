using System.Globalization;
using System.Net.Http.Json;
using Illumin360.Payments.Application.Abstractions;

namespace Illumin360.Payments.Infrastructure.Providers;

/// <summary>
/// Flutterwave v3 adapter — the payout-capable option (Subaccounts / Transfers API). Verified against
/// developer.flutterwave.com (2026-08-12). Amounts are <b>major units</b> in v3 (not minor). Auth is
/// <c>Bearer &lt;SECRET_KEY&gt;</c>, JSON.
///
/// Two verified caveats baked in below:
/// 1. <c>POST /payments</c> returns only <c>data.link</c> (a hosted-checkout URL) — there is NO transaction id
///    at creation, and it is a checkout session, not a real auth-hold. The numeric transaction id (needed to
///    refund) only exists after the payer pays + you call verify. So "hold" here starts a checkout; funding is
///    confirmed asynchronously (webhook/verify) — a follow-up for the async funding flow.
/// 2. <b>NAD / Namibia is NOT a Flutterwave settlement/payout corridor</b> (supported markets: NGN, GHS, KES,
///    ZAR, UGX, TZS, RWF, ZMW, XAF, XOF, EGP, MWK + card acquiring in UK/US/EU). Do not assume NAD works —
///    confirm the corridor with Flutterwave before enabling. Off by default; D2-gated.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="options">Provider options (BaseUrl e.g. https://api.flutterwave.com/v3, SecretKey FLWSECK-...).</param>
public sealed class FlutterwavePaymentProvider(HttpClient http, PaymentProviderOptions options) : IPaymentProvider
{
    private readonly HttpClient _http = http;
    private readonly PaymentProviderOptions _options = options;

    /// <inheritdoc />
    public Task<PaymentResult> CreateHoldAsync(string idempotencyKey, long amountMinor, string currency, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        // POST /payments creates a hosted-checkout session and returns only data.link (no id). We return our
        // own tx_ref as the reference; the numeric transaction id is resolved later via verify/webhook.
        var body = new
        {
            tx_ref = idempotencyKey,
            amount = (amountMinor / 100.0m).ToString(CultureInfo.InvariantCulture),
            currency,
            redirect_url = "https://illumin360.example/payments/return",
            customer = new { email = "payer@illumin360.example" },
        };
        return PostAsync("/payments", idempotencyKey, body, readLink: true, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PaymentResult> ReleaseAsync(ReleaseInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        // Transfer the released amount to the talent. Flutterwave transfers need account_bank + account_number;
        // we encode DestinationAccount as "bankCode:accountNumber" (fall back to treating it as the number).
        var parts = instruction.DestinationAccount.Split(':', 2);
        var bank = parts.Length == 2 ? parts[0] : string.Empty;
        var account = parts.Length == 2 ? parts[1] : instruction.DestinationAccount;

        var body = new
        {
            account_bank = bank,
            account_number = account,
            amount = instruction.AmountMinor / 100.0m,
            currency = instruction.Currency,
            reference = instruction.IdempotencyKey,
            narration = "Illumin360 milestone release",
        };
        return PostAsync("/transfers", instruction.IdempotencyKey, body, readLink: false, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PaymentResult> RefundAsync(RefundInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        // Refund is by the NUMERIC Flutterwave transaction id (resolved via verify), not our tx_ref.
        var body = new { amount = instruction.AmountMinor / 100.0m, comments = "Illumin360 milestone refund" };
        return PostAsync($"/transactions/{instruction.HoldReference}/refund", instruction.IdempotencyKey, body, readLink: false, cancellationToken);
    }

    private async Task<PaymentResult> PostAsync(string path, string fallbackReference, object body, bool readLink, CancellationToken cancellationToken)
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

        // Payloads are wrapped as { status, message, data: { ... } }. /payments has data.link only; /transfers
        // and /refund have data.id.
        var key = readLink ? "link" : "id";
        return new PaymentResult(true, ProviderJson.ReadString(text, key, parent: "data") ?? fallbackReference);
    }
}
