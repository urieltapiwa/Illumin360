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

namespace Illumin360.Professionals.IntegrationTests;

/// <summary>
/// Smoke tests for the auth-gated professional self-service endpoints against a real PostgreSQL via
/// Testcontainers (charter Part 14). JWT bearer is reconfigured to trust a local HS256 test key so the
/// RBAC ladder (401 → 403 → 200 → 409) is exercised programmatically instead of by manual login.
/// Requires a Docker daemon on the test host.
/// </summary>
public sealed class ProfessionalActionsAuthTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("illumin360_professionals")
        .WithUsername("illumin")
        .WithPassword("illumin_dev_pw")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await WaitForPostgresAsync();

        // The Professionals host reads its connection string eagerly at DI-registration time (inside
        // AddProfessionalsInfrastructure), which runs before WebApplicationFactory's ConfigureAppConfiguration
        // overrides are applied. An environment variable is folded into configuration by CreateBuilder BEFORE
        // that eager read and outranks appsettings.Development.json, so it is the only override that lands.
        Environment.SetEnvironmentVariable("ConnectionStrings__professionals", _postgres.GetConnectionString() + ";SSL Mode=Disable");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseTestAuth();
        });

        // Build + start the host now (runs the startup migration and demo seed against the container),
        // so the first test request already sees a seeded "me" professional with matches.
        _ = _factory.Server;
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__professionals", null);
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Applying_without_a_token_returns_401()
    {
        var client = _factory.CreateClient();
        var matchId = await FirstMatchIdAsync(client);

        var response = await client.PostAsync($"/v1/professionals/me/matches/{matchId}/apply", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Applying_without_a_client_role_returns_403()
    {
        var client = _factory.CreateClient();
        var matchId = await FirstMatchIdAsync(client);
        Authorize(client, ["account.viewer"]);

        var response = await client.PostAsync($"/v1/professionals/me/matches/{matchId}/apply", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Professional_can_apply_then_a_re_apply_conflicts()
    {
        var client = _factory.CreateClient();
        var matchId = await FirstMatchIdAsync(client);
        Authorize(client, ["client.user"]);

        var first = await client.PostAsync($"/v1/professionals/me/matches/{matchId}/apply", content: null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsync($"/v1/professionals/me/matches/{matchId}/apply", content: null);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Role_scores_rank_a_matching_city_role_above_an_unrelated_one()
    {
        var client = _factory.CreateClient();

        // The seeded demo professional is a Windhoek software developer; score two contrasting roles.
        var roles = new[]
        {
            new { id = Guid.NewGuid(), title = "Software Developer", city = "Windhoek", industry = "Technology" },
            new { id = Guid.NewGuid(), title = "Chef", city = "Walvis Bay", industry = "Hospitality" },
        };

        var response = await client.PostAsJsonAsync("/v1/professionals/me/role-scores", roles);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var scores = await response.Content.ReadFromJsonAsync<List<RoleScore>>();
        scores.Should().HaveCount(2);
        var dev = scores!.Single(s => s.Id == roles[0].id).Score;
        var chef = scores!.Single(s => s.Id == roles[1].id).Score;
        dev.Should().BeGreaterThan(chef);
    }

    [Fact]
    public async Task Professional_can_toggle_availability()
    {
        var client = _factory.CreateClient();
        Authorize(client, ["client.user"]);

        var response = await client.PostAsJsonAsync(
            "/v1/professionals/me/availability",
            new { availability = "Not looking" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
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

    private sealed record RoleScore(Guid Id, int Score);

    private static void Authorize(HttpClient client, string[] roles) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.ForRoles(roles));

    private static async Task<Guid> FirstMatchIdAsync(HttpClient client)
    {
        var dashboard = await client.GetStringAsync("/v1/professionals/me");
        using var doc = JsonDocument.Parse(dashboard);
        var id = doc.RootElement.GetProperty("matches")[0].GetProperty("id").GetGuid();
        return id;
    }
}
