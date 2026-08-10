using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Illumin360.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Illumin360.Students.IntegrationTests;

/// <summary>
/// Smoke tests for the auth-gated student self-service endpoints against a real PostgreSQL via
/// Testcontainers (charter Part 14). JWT bearer is reconfigured (via the shared TestSupport helper) to
/// trust a local HS256 key so the RBAC ladder (401 → 403 → 200 → 409) is exercised programmatically
/// instead of by manual login. Requires a Docker daemon on the test host.
/// </summary>
public sealed class StudentActionsAuthTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("illumin360_students")
        .WithUsername("illumin")
        .WithPassword("illumin_dev_pw")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await WaitForPostgresAsync();

        // The Students host reads its connection string eagerly at DI-registration time (inside
        // AddStudentsInfrastructure), which runs before WebApplicationFactory's ConfigureAppConfiguration
        // overrides are applied. An environment variable is folded into configuration by CreateBuilder BEFORE
        // that eager read and outranks appsettings.Development.json, so it is the only override that lands.
        Environment.SetEnvironmentVariable("ConnectionStrings__students", _postgres.GetConnectionString() + ";SSL Mode=Disable");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseTestAuth();
        });

        // Build + start the host now (runs the startup migration and demo seed against the container),
        // so the first test request already sees a seeded "me" student with matches.
        _ = _factory.Server;
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__students", null);
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Applying_without_a_token_returns_401()
    {
        var client = _factory.CreateClient();
        var matchId = await FirstMatchIdAsync(client);

        var response = await client.PostAsync($"/v1/students/me/matches/{matchId}/apply", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Applying_without_a_client_role_returns_403()
    {
        var client = _factory.CreateClient();
        var matchId = await FirstMatchIdAsync(client);
        Authorize(client, ["account.viewer"]);

        var response = await client.PostAsync($"/v1/students/me/matches/{matchId}/apply", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Student_can_apply_then_a_re_apply_conflicts()
    {
        var client = _factory.CreateClient();
        var matchId = await FirstMatchIdAsync(client);
        Authorize(client, ["client.user"]);

        var first = await client.PostAsync($"/v1/students/me/matches/{matchId}/apply", content: null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsync($"/v1/students/me/matches/{matchId}/apply", content: null);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Student_can_toggle_availability()
    {
        var client = _factory.CreateClient();
        Authorize(client, ["client.user"]);

        var response = await client.PostAsJsonAsync(
            "/v1/students/me/availability",
            new { availability = "Not looking" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static void Authorize(HttpClient client, string[] roles) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.ForRoles(roles));

    private static async Task<Guid> FirstMatchIdAsync(HttpClient client)
    {
        var dashboard = await client.GetStringAsync("/v1/students/me");
        using var doc = JsonDocument.Parse(dashboard);
        return doc.RootElement.GetProperty("matches")[0].GetProperty("id").GetGuid();
    }

    private async Task WaitForPostgresAsync()
    {
        var connectionString = _postgres.GetConnectionString() + ";SSL Mode=Disable";
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();
                return;
            }
            catch (NpgsqlException) when (attempt < 20)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }
    }
}
