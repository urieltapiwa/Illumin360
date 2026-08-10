using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
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

        // Warm the mapped port before the host boots. Docker Desktop's port-forwarding can reset the first
        // TCP connection after a container starts, which surfaces as an Npgsql "read past end of stream";
        // retrying a plain connection here clears that race before the startup migration runs.
        await WaitForPostgresAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:professionals"] = _postgres.GetConnectionString() + ";SSL Mode=Disable",
            }));

            // Trust the local HS256 test key instead of Keycloak's JWKS, so tokens can be minted offline.
            b.ConfigureTestServices(services =>
            {
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
                {
                    o.Authority = null;
                    o.MetadataAddress = null!;
                    o.RequireHttpsMetadata = false;
                    o.Configuration = new OpenIdConnectConfiguration();
                    o.TokenValidationParameters.ValidateIssuer = false;
                    o.TokenValidationParameters.ValidateAudience = false;
                    o.TokenValidationParameters.ValidateLifetime = true;
                    o.TokenValidationParameters.ValidateIssuerSigningKey = true;
                    o.TokenValidationParameters.IssuerSigningKey = TestToken.SigningKey;
                    o.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                    o.TokenValidationParameters.NameClaimType = "preferred_username";
                });
            });
        });

        // Build + start the host now (runs the startup migration and demo seed against the container),
        // so the first test request already sees a seeded "me" professional with matches.
        _ = _factory.Server;
    }

    public async Task DisposeAsync()
    {
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

    private static void Authorize(HttpClient client, string[] roles) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestToken.ForRoles(roles));

    private static async Task<Guid> FirstMatchIdAsync(HttpClient client)
    {
        var dashboard = await client.GetStringAsync("/v1/professionals/me");
        using var doc = JsonDocument.Parse(dashboard);
        var id = doc.RootElement.GetProperty("matches")[0].GetProperty("id").GetGuid();
        return id;
    }
}
