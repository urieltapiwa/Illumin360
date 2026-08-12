using System.Text;
using System.Xml.Linq;
using Illumin360.Payments.Application.Abstractions;

namespace Illumin360.Payments.Infrastructure.Providers;

/// <summary>
/// DPO Group adapter (Southern/East-African acquiring, good NAD coverage). DPO uses a single XML endpoint
/// (<c>/API/v6/</c>): <c>createToken</c> holds funds (returns a TransToken), <c>chargeToken*</c>/verify settles
/// (release), and <c>refundToken</c> reverses. The company token comes from
/// <see cref="PaymentProviderOptions.Extra"/>.
///
/// SCAFFOLD — DPO's XML API is materially different from the JSON providers; the request/response shapes here
/// follow the documented v6 API but MUST be validated against DPO's sandbox before enabling. DPO is
/// collection-first, so third-party payouts to talent are weaker than Flutterwave — factor that into the D1
/// choice. Off by default; D2-gated.
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
        var xml = new XElement(
            "API3G",
            new XElement("CompanyToken", _options.Extra),
            new XElement("Request", "createToken"),
            new XElement(
                "Transaction",
                new XElement("PaymentAmount", (amountMinor / 100.0m).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new XElement("PaymentCurrency", currency),
                new XElement("CompanyRef", idempotencyKey)));
        return PostAsync(xml, "TransToken", idempotencyKey, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PaymentResult> ReleaseAsync(ReleaseInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        // Settle the held token. DPO is collection-first; disbursing to the talent (DestinationAccount) is a
        // separate payout step — validate against the DPO sandbox.
        var xml = new XElement(
            "API3G",
            new XElement("CompanyToken", _options.Extra),
            new XElement("Request", "verifyToken"),
            new XElement("TransactionToken", instruction.HoldReference));
        return PostAsync(xml, "Result", instruction.IdempotencyKey, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PaymentResult> RefundAsync(RefundInstruction instruction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instruction);
        var xml = new XElement(
            "API3G",
            new XElement("CompanyToken", _options.Extra),
            new XElement("Request", "refundToken"),
            new XElement("TransactionToken", instruction.HoldReference),
            new XElement("refundAmount", (instruction.AmountMinor / 100.0m).ToString(System.Globalization.CultureInfo.InvariantCulture)));
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
            if (result is not null && result != "000" && readElement == "Result")
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
