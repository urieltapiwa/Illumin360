using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Illumin360.Payments.Application.Abstractions;

namespace Illumin360.Payments.Infrastructure.Providers;

/// <summary>
/// DPO Group (Direct Pay Online) API3G v6 adapter. Verified against DPO's Confluence API docs (2026-08-12).
/// Single XML endpoint <c>POST /API/v6/</c> (same host for sandbox + production; the CompanyToken decides which).
/// DPO operates in Namibia + Southern/East Africa for <b>collection</b>, but its public API is
/// <b>collection/acquiring only — no third-party payout</b>: refunds go to the original payer, and there is no
/// documented disburse-to-seller call. So this adapter can create a payment token + refund-to-payer, but
/// <b>cannot pay the talent</b> — <see cref="ReleaseAsync"/> returns an explicit not-supported result. Also
/// note <c>createToken</c> is NOT an auth-hold: it creates a payment request the payer then completes;
/// "holding" is your own ledger against a verified payment. Off by default; D2-gated.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="options">Provider options (BaseUrl e.g. https://secure.3gdirectpay.com, Extra = company token).</param>
public sealed class DpoPaymentProvider(HttpClient http, PaymentProviderOptions options) : IPaymentProvider
{
    private readonly HttpClient _http = http;
    private readonly PaymentProviderOptions _options = options;

    /// <inheritdoc />
    public Task<PaymentResult> CreateHoldAsync(string idempotencyKey, long amountMinor, string currency, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        // createToken requires a Transaction block (amount/currency/ref) AND at least one Services/Service.
        var xml = new XElement(
            "API3G",
            new XElement("CompanyToken", _options.Extra),
            new XElement("Request", "createToken"),
            new XElement(
                "Transaction",
                new XElement("PaymentAmount", (amountMinor / 100.0m).ToString(CultureInfo.InvariantCulture)),
                new XElement("PaymentCurrency", currency),
                new XElement("CompanyRef", idempotencyKey),
                new XElement("CompanyRefUnique", "1"),
                new XElement("RedirectURL", "https://illumin360.example/payments/return"),
                new XElement("BackURL", "https://illumin360.example/payments/cancel")),
            new XElement(
                "Services",
                new XElement(
                    "Service",
                    new XElement("ServiceType", _options.Extra), // account-specific service-type id (placeholder)
                    new XElement("ServiceDescription", "Illumin360 milestone funding"),
                    new XElement("ServiceDate", "2026/01/01 00:00"))));
        return PostAsync(xml, "TransToken", idempotencyKey, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PaymentResult> ReleaseAsync(ReleaseInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        // DPO's public API is collection-only — no disburse-to-third-party call.
        return Task.FromResult(new PaymentResult(false, string.Empty, "DPO API is collection-only — no third-party payout; disburse to the talent out-of-band."));
    }

    /// <inheritdoc />
    public Task<PaymentResult> RefundAsync(RefundInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        // refundToken requires BOTH refundAmount and refundDetails (note the lowercase leading 'r').
        var xml = new XElement(
            "API3G",
            new XElement("CompanyToken", _options.Extra),
            new XElement("Request", "refundToken"),
            new XElement("TransactionToken", instruction.HoldReference),
            new XElement("refundAmount", (instruction.AmountMinor / 100.0m).ToString(CultureInfo.InvariantCulture)),
            new XElement("refundDetails", "Illumin360 milestone refund"));
        return PostAsync(xml, "Result", instruction.IdempotencyKey, cancellationToken);
    }

    private async Task<PaymentResult> PostAsync(XElement xml, string readElement, string idempotencyKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl!.TrimEnd('/')}/API/v6/")
        {
            Content = new StringContent(xml.ToString(), Encoding.UTF8, "application/xml"),
        };

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return new PaymentResult(false, string.Empty, $"DPO {(int)response.StatusCode}: {text}");
        }

        try
        {
            var doc = XDocument.Parse(text);
            var result = doc.Root?.Element("Result")?.Value;

            // DPO signals success with Result code "000".
            if (result is not null && result != "000")
            {
                return new PaymentResult(false, string.Empty, $"DPO result {result}: {doc.Root?.Element("ResultExplanation")?.Value}");
            }

            var reference = doc.Root?.Element(readElement)?.Value;
            return new PaymentResult(true, string.IsNullOrWhiteSpace(reference) ? idempotencyKey : reference);
        }
        catch (System.Xml.XmlException)
        {
            return new PaymentResult(false, string.Empty, "DPO: unparseable XML response.");
        }
    }
}
