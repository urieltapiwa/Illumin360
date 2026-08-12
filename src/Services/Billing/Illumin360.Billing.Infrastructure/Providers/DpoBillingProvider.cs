using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Illumin360.Billing.Application.Abstractions;

namespace Illumin360.Billing.Infrastructure.Providers;

/// <summary>
/// DPO Group (Direct Pay Online) API3G v6 recurring adapter. Verified against DPO's Confluence API docs
/// (2026-08-12). Single XML endpoint <c>POST /API/v6/</c>. DPO is <b>the NAD-capable option</b> for Namibian
/// customers — the one provider of the three that can bill in NAD.
/// <para>
/// Recurring model: <see cref="StartSubscriptionAsync"/> issues a <c>createToken</c> with an
/// <c>&lt;AllowRecurrent&gt;1&lt;/AllowRecurrent&gt;</c> flag; DPO returns a <c>TransToken</c> the payer completes
/// once, after which subsequent cycles are collected with <c>chargeTokenAuth</c> against that stored token
/// (<see cref="ChargeAsync"/>). <see cref="CancelSubscriptionAsync"/> issues <c>cancelToken</c>.
/// </para>
/// Off by default; going live needs recurring enabled on the DPO account + the company token + D2 sign-off.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="options">Provider options (BaseUrl e.g. https://secure.3gdirectpay.com, Extra = company token).</param>
public sealed class DpoBillingProvider(HttpClient http, BillingProviderOptions options) : IBillingProvider
{
    private readonly HttpClient _http = http;
    private readonly BillingProviderOptions _options = options;

    /// <inheritdoc />
    public Task<BillingResult> StartSubscriptionAsync(string idempotencyKey, long amountMinor, string currency, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

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
                new XElement("AllowRecurrent", "1"),
                new XElement("RedirectURL", "https://illumin360.example/billing/return"),
                new XElement("BackURL", "https://illumin360.example/billing/cancel")),
            new XElement(
                "Services",
                new XElement(
                    "Service",
                    new XElement("ServiceType", _options.Extra),
                    new XElement("ServiceDescription", "Illumin360 subscription"),
                    new XElement("ServiceDate", "2026/01/01 00:00"))));
        return PostAsync(xml, "TransToken", idempotencyKey, cancellationToken);
    }

    /// <inheritdoc />
    public Task<BillingResult> ChargeAsync(string idempotencyKey, string providerRef, long amountMinor, string currency, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerRef);

        // chargeTokenAuth re-charges the stored recurring token for one cycle.
        var xml = new XElement(
            "API3G",
            new XElement("CompanyToken", _options.Extra),
            new XElement("Request", "chargeTokenAuth"),
            new XElement("TransactionToken", providerRef),
            new XElement("PaymentAmount", (amountMinor / 100.0m).ToString(CultureInfo.InvariantCulture)),
            new XElement("PaymentCurrency", currency),
            new XElement("CompanyRef", idempotencyKey));
        return PostAsync(xml, "TransactionToken", idempotencyKey, cancellationToken);
    }

    /// <inheritdoc />
    public Task<BillingResult> CancelSubscriptionAsync(string providerRef, CancellationToken cancellationToken)
    {
        var xml = new XElement(
            "API3G",
            new XElement("CompanyToken", _options.Extra),
            new XElement("Request", "cancelToken"),
            new XElement("TransactionToken", providerRef));
        return PostAsync(xml, "Result", providerRef, cancellationToken);
    }

    private async Task<BillingResult> PostAsync(XElement xml, string readElement, string fallbackReference, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl!.TrimEnd('/')}/API/v6/")
        {
            Content = new StringContent(xml.ToString(), Encoding.UTF8, "application/xml"),
        };

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return new BillingResult(false, string.Empty, Error: $"DPO {(int)response.StatusCode}: {text}");
        }

        try
        {
            var doc = XDocument.Parse(text);
            var result = doc.Root?.Element("Result")?.Value;
            if (result is not null && result != "000")
            {
                return new BillingResult(false, string.Empty, Error: $"DPO result {result}: {doc.Root?.Element("ResultExplanation")?.Value}");
            }

            var reference = doc.Root?.Element(readElement)?.Value;
            return new BillingResult(true, string.IsNullOrWhiteSpace(reference) ? fallbackReference : reference);
        }
        catch (System.Xml.XmlException)
        {
            return new BillingResult(false, string.Empty, Error: "DPO: unparseable XML response.");
        }
    }
}
