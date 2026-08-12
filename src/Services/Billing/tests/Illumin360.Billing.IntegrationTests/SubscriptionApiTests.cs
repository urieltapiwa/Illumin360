using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Illumin360.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace Illumin360.Billing.IntegrationTests;

/// <summary>
/// End-to-end subscription lifecycle for the Billing service against a real PostgreSQL via Testcontainers:
/// create a plan → subscribe a customer → check entitlements → list the paid invoice → cancel → entitlements
/// revoked. Drives the whole model + Fake provider over HTTP. Requires a Docker daemon on the test host.
/// </summary>
public sealed class SubscriptionApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("illumin360_billing")
        .WithUsername("illumin")
        .WithPassword("illumin_dev_pw")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__billing", _postgres.GetConnectionString() + ";SSL Mode=Disable");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("Billing:Enabled", "false"); // don't run the scheduler during the test
            b.UseTestAuth();
        });
        _ = _factory.Server;
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__billing", null);
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Create_plan_subscribe_entitle_invoice_cancel()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.ForRoles(["admin.superuser"]));

        var plan = await (await client.PostAsJsonAsync("/v1/billing/plans", new { code = "pro", name = "Pro", priceMinor = 50000, currency = "NAD", interval = "Monthly", features = new[] { "ats.advanced", "reports.export" } })).Content.ReadFromJsonAsync<PlanDto>();
        plan!.Code.Should().Be("pro");

        var customer = Guid.NewGuid();
        var sub = await (await client.PostAsJsonAsync("/v1/billing/subscriptions", new { customerId = customer, planCode = "pro" })).Content.ReadFromJsonAsync<SubscriptionDto>();
        sub!.Status.Should().Be("Active");

        var ent = await client.GetFromJsonAsync<EntitlementsDto>($"/v1/billing/entitlements/{customer}");
        ent!.PlanCode.Should().Be("pro");
        ent.Features.Should().Contain("ats.advanced");

        var invoices = await client.GetFromJsonAsync<List<InvoiceDto>>($"/v1/billing/subscriptions/{customer}/invoices");
        invoices!.Should().ContainSingle().Which.Status.Should().Be("Paid");

        (await client.PostAsync($"/v1/billing/subscriptions/{customer}/cancel", content: null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await client.GetFromJsonAsync<EntitlementsDto>($"/v1/billing/entitlements/{customer}");
        after!.Features.Should().BeEmpty();
    }

    private sealed record PlanDto(Guid Id, string Code, string Name, long PriceMinor, string Currency, string Interval, List<string> Features);

    private sealed record SubscriptionDto(Guid Id, Guid CustomerId, Guid PlanId, string PlanCode, string Status, DateTimeOffset CurrentPeriodEnd, string? CheckoutUrl);

    private sealed record InvoiceDto(Guid Id, long AmountMinor, string Currency, string Status, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, DateTimeOffset? PaidAt);

    private sealed record EntitlementsDto(Guid CustomerId, string? PlanCode, string Status, List<string> Features);
}
