using System.Net;
using System.Text;
using FluentAssertions;
using Illumin360.Payments.Application.Abstractions;
using Illumin360.Payments.Infrastructure.Providers;
using Xunit;

namespace Illumin360.Payments.UnitTests;

public class ProviderAdapterTests
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
    public void UseReal_requires_a_real_provider_enabled_and_a_base_url()
    {
        new PaymentProviderOptions().UseReal.Should().BeFalse();
        new PaymentProviderOptions { Provider = PaymentProviderKind.Flutterwave }.UseReal.Should().BeFalse();
        new PaymentProviderOptions { Provider = PaymentProviderKind.Flutterwave, Enabled = true }.UseReal.Should().BeFalse();
        new PaymentProviderOptions { Provider = PaymentProviderKind.Flutterwave, Enabled = true, BaseUrl = "https://api" }.UseReal.Should().BeTrue();
    }

    [Fact]
    public async Task Stripe_hold_posts_a_manual_capture_intent_with_idempotency_and_parses_the_id()
    {
        var recorder = new Recorder(_ => (HttpStatusCode.OK, """{"id":"pi_123","status":"requires_capture"}"""));
        var provider = new StripeConnectPaymentProvider(new HttpClient(recorder), new PaymentProviderOptions { BaseUrl = "https://api.stripe.com", SecretKey = "sk_test" });

        var result = await provider.CreateHoldAsync("ms-1", 500000, "NAD", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Reference.Should().Be("pi_123");
        var req = recorder.Requests[0];
        req.RequestUri!.AbsolutePath.Should().Be("/v1/payment_intents");
        req.Headers.Authorization!.ToString().Should().Be("Bearer sk_test");
        req.Headers.Contains("Idempotency-Key").Should().BeTrue();
        recorder.Bodies[0].Should().Contain("capture_method=manual").And.Contain("currency=nad");
    }

    [Fact]
    public async Task Stripe_release_captures_the_intent()
    {
        var recorder = new Recorder(_ => (HttpStatusCode.OK, """{"id":"pi_123","status":"succeeded"}"""));
        var provider = new StripeConnectPaymentProvider(new HttpClient(recorder), new PaymentProviderOptions { BaseUrl = "https://api.stripe.com", SecretKey = "sk_test" });

        await provider.ReleaseAsync(new ReleaseInstruction("ms-1", "pi_123", 500000, "NAD", "acct_talent"), CancellationToken.None);

        recorder.Requests[0].RequestUri!.AbsolutePath.Should().Be("/v1/payment_intents/pi_123/capture");
    }

    [Fact]
    public async Task Stripe_surfaces_a_provider_error()
    {
        var recorder = new Recorder(_ => (HttpStatusCode.BadRequest, """{"error":{"message":"no such intent"}}"""));
        var provider = new StripeConnectPaymentProvider(new HttpClient(recorder), new PaymentProviderOptions { BaseUrl = "https://api.stripe.com", SecretKey = "sk_test" });

        var result = await provider.RefundAsync(new RefundInstruction("ms-1", "pi_x", 500000, "NAD"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("400");
    }

    [Fact]
    public async Task Flutterwave_hold_starts_a_checkout_and_returns_the_hosted_link()
    {
        // /payments returns only data.link (a hosted checkout URL) — no transaction id at creation.
        var recorder = new Recorder(_ => (HttpStatusCode.OK, """{"status":"success","message":"Hosted Link","data":{"link":"https://checkout.flutterwave.com/pay/flwlnk-01"}}"""));
        var provider = new FlutterwavePaymentProvider(new HttpClient(recorder), new PaymentProviderOptions { BaseUrl = "https://api.flutterwave.com/v3", SecretKey = "FLWSECK" });

        var result = await provider.CreateHoldAsync("ms-1", 500000, "NAD", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Reference.Should().Be("https://checkout.flutterwave.com/pay/flwlnk-01");
        recorder.Requests[0].RequestUri!.AbsolutePath.Should().Be("/v3/payments");
        recorder.Requests[0].Headers.Authorization!.ToString().Should().Be("Bearer FLWSECK");
        // v3 amounts are major units (500000 minor -> 5000.00).
        recorder.Bodies[0].Should().Contain("\"tx_ref\":\"ms-1\"").And.Contain("\"currency\":\"NAD\"").And.Contain("5000");
    }

    [Fact]
    public async Task Collection_only_providers_report_no_third_party_payout_on_release()
    {
        var release = new ReleaseInstruction("ms-1", "hold-1", 300000, "NAD", "acct_talent");

        var ngenius = await new NGeniusPaymentProvider(new HttpClient(new Recorder(_ => (HttpStatusCode.OK, "{}"))), new PaymentProviderOptions { BaseUrl = "https://api-gateway.sandbox.ngenius-payments.com", SecretKey = "k", Extra = "outlet" }).ReleaseAsync(release, CancellationToken.None);
        ngenius.Success.Should().BeFalse();
        ngenius.Error.Should().Contain("no third-party payout");

        var dpo = await new DpoPaymentProvider(new HttpClient(new Recorder(_ => (HttpStatusCode.OK, "<API3G><Result>000</Result></API3G>"))), new PaymentProviderOptions { BaseUrl = "https://secure.3gdirectpay.com", SecretKey = "k", Extra = "ct" }).ReleaseAsync(release, CancellationToken.None);
        dpo.Success.Should().BeFalse();
        dpo.Error.Should().Contain("collection-only");
    }

    [Fact]
    public async Task Flutterwave_release_transfers_the_amount_to_the_destination_account()
    {
        var recorder = new Recorder(_ => (HttpStatusCode.OK, """{"status":"success","data":{"id":"tr-7"}}"""));
        var provider = new FlutterwavePaymentProvider(new HttpClient(recorder), new PaymentProviderOptions { BaseUrl = "https://api.flutterwave.com/v3", SecretKey = "FLWSECK" });

        var result = await provider.ReleaseAsync(new ReleaseInstruction("ms-1", "flw-9", 300000, "NAD", "sub_talent_42"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Reference.Should().Be("tr-7");
        recorder.Requests[0].RequestUri!.AbsolutePath.Should().Be("/v3/transfers");
        recorder.Bodies[0].Should().Contain("sub_talent_42").And.Contain("\"amount\":3000");
    }
}
