using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace Illumin360.Candidates.IntegrationTests;

/// <summary>
/// Component/API tests against a real PostgreSQL via Testcontainers (charter Part 14).
/// Requires a Docker daemon on the test host.
/// </summary>
public sealed class CandidatesApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("illumin360_candidates")
        .WithUsername("illumin")
        .WithPassword("illumin_dev_pw")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__candidates", null);
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Live_probe_returns_200()
    {
        // The host reads its connection string eagerly at DI-registration time, before
        // WebApplicationFactory's ConfigureAppConfiguration overrides apply; an environment variable is
        // folded in by CreateBuilder before that read and outranks appsettings.Development.json.
        Environment.SetEnvironmentVariable("ConnectionStrings__candidates", _postgres.GetConnectionString() + ";SSL Mode=Disable");

        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
        });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
