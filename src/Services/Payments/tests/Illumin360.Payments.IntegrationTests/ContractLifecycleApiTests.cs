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

namespace Illumin360.Payments.IntegrationTests;

/// <summary>
/// End-to-end lifecycle test for the Payments service against a real PostgreSQL via Testcontainers: create a
/// contract → add a milestone → activate → fund → submit → approve, driving the full state machine +
/// ledger + (fake) provider over HTTP, and asserting the contract auto-completes and the ledger records the
/// fund + release movements. Requires a Docker daemon on the test host.
/// </summary>
public sealed class ContractLifecycleApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("illumin360_payments")
        .WithUsername("illumin")
        .WithPassword("illumin_dev_pw")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__payments", _postgres.GetConnectionString() + ";SSL Mode=Disable");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseTestAuth();
        });
        _ = _factory.Server;
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__payments", null);
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Full_fixed_price_lifecycle_completes_and_ledgers_the_movements()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.ForRoles(["client.user"]));

        var contract = await (await client.PostAsJsonAsync("/v1/payments/contracts", new { clientId = Guid.NewGuid(), talentId = Guid.NewGuid(), requestId = (Guid?)null, title = "Website build", currency = "NAD" })).Content.ReadFromJsonAsync<ContractDto>();
        contract!.Status.Should().Be("Draft");

        var milestone = await (await client.PostAsJsonAsync($"/v1/payments/contracts/{contract.Id}/milestones", new { title = "Phase 1", amountMinor = 500000L })).Content.ReadFromJsonAsync<MilestoneDto>();
        milestone!.Status.Should().Be("Pending");

        // Can't fund before the contract is active.
        (await client.PostAsync($"/v1/payments/milestones/{milestone.Id}/fund", content: null)).StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await client.PostAsync($"/v1/payments/contracts/{contract.Id}/activate", content: null)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.PostAsync($"/v1/payments/milestones/{milestone.Id}/fund", content: null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync($"/v1/payments/milestones/{milestone.Id}/submit", content: null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync($"/v1/payments/milestones/{milestone.Id}/approve", content: null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await client.GetFromJsonAsync<ContractDetail>($"/v1/payments/contracts/{contract.Id}");
        detail!.Contract.Status.Should().Be("Completed");
        detail.Milestones.Should().ContainSingle().Which.Status.Should().Be("Approved");
        detail.Movements.Select(m => m.Kind).Should().BeEquivalentTo(["Fund", "Release"]);
    }

    private sealed record ContractDto(Guid Id, Guid ClientId, Guid TalentId, Guid? RequestId, string Title, string Currency, string Status, DateTimeOffset CreatedAt);

    private sealed record MilestoneDto(Guid Id, int Order, string Title, long AmountMinor, string Status, DateTimeOffset? FundedAt, DateTimeOffset? SubmittedAt, DateTimeOffset? DecidedAt);

    private sealed record MovementDto(Guid Id, Guid MilestoneId, string Kind, long AmountMinor, string Currency, DateTimeOffset CreatedAt);

    private sealed record ContractDetail(ContractDto Contract, List<MilestoneDto> Milestones, List<MovementDto> Movements);
}
