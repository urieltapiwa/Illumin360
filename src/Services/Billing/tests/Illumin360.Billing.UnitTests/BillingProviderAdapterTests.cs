using System.Net;
using System.Text;
using FluentAssertions;
using Illumin360.Billing.Infrastructure.Providers;
using Xunit;

namespace Illumin360.Billing.UnitTests;

public class BillingProviderAdapterTests
{
    private sealed class Recorder(Func<HttpRequestMessage, (HttpStatusCode, string)> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            var (code, body) = respond(request);
            return new HttpResponseMessage(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    [Fact]
    public void UseReal_requires_a_real_provider_the_gate_and_a_base_url()
    {
        new BillingProviderOptions().UseReal.Should().BeFalse();
        new BillingProviderOptions { Provider = BillingProviderKind.Dpo }.UseReal.Should().BeFalse();
        new BillingProviderOptions { Provider = BillingProviderKind.Dpo, ProviderEnabled = true }.UseReal.Should().BeFalse();
        new BillingProviderOptions { Provider = BillingProviderKind.Dpo, ProviderEnabled = true, BaseUrl = "https://api" }.UseReal.Should().BeTrue();
    }

    [Fact]
    public async Task Flutterwave_start_opens_a_hosted_checkout_for_the_first_payment()
    {
        var recorder = new Recorder(_ => (HttpStatusCode.OK, """{"status":"success","data":{"link":"https://checkout.flutterwave.com/pay/flwlnk-9"}}"""));
        var provider = new FlutterwaveBillingProvider(new HttpClient(recorder), new BillingProviderOptions { BaseUrl = "https://api.flutterwave.com/v3", SecretKey = "FLWSECK" });

        var result = await provider.StartSubscriptionAsync("sub-1", 50000, "ZAR", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.CheckoutUrl.Should().Be("https://checkout.flutterwave.com/pay/flwlnk-9");
        recorder.Requests[0].RequestUri!.AbsolutePath.Should().Be("/v3/payments");
        recorder.Requests[0].Headers.Authorization!.ToString().Should().Be("Bearer FLWSECK");
        // v3 amounts are major units (50000 minor -> 500).
        recorder.Bodies[0].Should().Contain("\"tx_ref\":\"sub-1\"").And.Contain("\"currency\":\"ZAR\"").And.Contain("500");
    }

    [Fact]
    public async Task Flutterwave_charge_recharges_the_stored_token()
    {
        var recorder = new Recorder(_ => (HttpStatusCode.OK, """{"status":"success","data":{"flw_ref":"FLW-REF-7"}}"""));
        var provider = new FlutterwaveBillingProvider(new HttpClient(recorder), new BillingProviderOptions { BaseUrl = "https://api.flutterwave.com/v3", SecretKey = "FLWSECK" });

        var result = await provider.ChargeAsync("inv-1", "tok_abc", 50000, "ZAR", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Reference.Should().Be("FLW-REF-7");
        recorder.Requests[0].RequestUri!.AbsolutePath.Should().Be("/v3/tokenized-charges");
        recorder.Bodies[0].Should().Contain("tok_abc");
    }

    [Fact]
    public async Task Flutterwave_surfaces_a_provider_error()
    {
        var recorder = new Recorder(_ => (HttpStatusCode.BadRequest, """{"message":"invalid token"}"""));
        var provider = new FlutterwaveBillingProvider(new HttpClient(recorder), new BillingProviderOptions { BaseUrl = "https://api.flutterwave.com/v3", SecretKey = "FLWSECK" });

        var result = await provider.ChargeAsync("inv-1", "tok_x", 50000, "ZAR", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("400");
    }

    [Fact]
    public async Task Dpo_start_creates_a_recurring_token_in_nad()
    {
        var recorder = new Recorder(_ => (HttpStatusCode.OK, "<?xml version=\"1.0\"?><API3G><Result>000</Result><TransToken>DPO-TOK-1</TransToken></API3G>"));
        var provider = new DpoBillingProvider(new HttpClient(recorder), new BillingProviderOptions { BaseUrl = "https://secure.3gdirectpay.com", Extra = "company-token" });

        var result = await provider.StartSubscriptionAsync("sub-1", 50000, "NAD", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Reference.Should().Be("DPO-TOK-1");
        recorder.Requests[0].RequestUri!.AbsolutePath.Should().Be("/API/v6/");
        recorder.Bodies[0].Should().Contain("createToken").And.Contain("<AllowRecurrent>1</AllowRecurrent>").And.Contain("<PaymentCurrency>NAD</PaymentCurrency>");
    }

    [Fact]
    public async Task Dpo_charge_uses_chargeTokenAuth_against_the_stored_token()
    {
        var recorder = new Recorder(_ => (HttpStatusCode.OK, "<API3G><Result>000</Result><TransactionToken>DPO-TOK-1</TransactionToken></API3G>"));
        var provider = new DpoBillingProvider(new HttpClient(recorder), new BillingProviderOptions { BaseUrl = "https://secure.3gdirectpay.com", Extra = "company-token" });

        var result = await provider.ChargeAsync("inv-1", "DPO-TOK-1", 50000, "NAD", CancellationToken.None);

        result.Success.Should().BeTrue();
        recorder.Bodies[0].Should().Contain("chargeTokenAuth").And.Contain("<TransactionToken>DPO-TOK-1</TransactionToken>");
    }

    [Fact]
    public async Task Dpo_surfaces_a_non_zero_result_code()
    {
        var recorder = new Recorder(_ => (HttpStatusCode.OK, "<API3G><Result>904</Result><ResultExplanation>Token not found</ResultExplanation></API3G>"));
        var provider = new DpoBillingProvider(new HttpClient(recorder), new BillingProviderOptions { BaseUrl = "https://secure.3gdirectpay.com", Extra = "ct" });

        var result = await provider.ChargeAsync("inv-1", "missing", 50000, "NAD", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("904");
    }

    [Fact]
    public async Task NGenius_start_authenticates_then_vaults_the_card()
    {
        var recorder = new Recorder(req => req.RequestUri!.AbsolutePath.EndsWith("access-token", StringComparison.Ordinal)
            ? (HttpStatusCode.OK, """{"access_token":"at-123"}""")
            : (HttpStatusCode.OK, """{"reference":"ord-1","_links":{"payment":{"href":"https://pay.ngenius/ord-1"}}}"""));
        var provider = new NGeniusBillingProvider(new HttpClient(recorder), new BillingProviderOptions { BaseUrl = "https://api-gateway.sandbox.ngenius-payments.com", SecretKey = "apikey", Extra = "outlet-9" });

        var result = await provider.StartSubscriptionAsync("sub-1", 5000, "AED", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Reference.Should().Be("ord-1");
        recorder.Requests[0].RequestUri!.AbsolutePath.Should().EndWith("/identity/auth/access-token");
        recorder.Requests[1].RequestUri!.AbsolutePath.Should().Be("/transactions/outlets/outlet-9/orders");
        recorder.Requests[1].Headers.Authorization!.ToString().Should().Be("Bearer at-123");
        recorder.Bodies[1].Should().Contain("recapture").And.Contain("\"currencyCode\":\"AED\"");
    }
}
