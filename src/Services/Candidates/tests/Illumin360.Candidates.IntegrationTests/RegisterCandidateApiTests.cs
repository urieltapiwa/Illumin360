using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Testcontainers.PostgreSql;
using Xunit;

namespace Illumin360.Candidates.IntegrationTests;

/// <summary>
/// Write-side component tests: register a candidate and read it back through the API,
/// against a real PostgreSQL via Testcontainers (charter Part 14). Requires Docker.
/// </summary>
public sealed class RegisterCandidateApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("illumin360_candidates")
        .WithUsername("illumin")
        .WithPassword("illumin_dev_pw")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // The host reads its connection string eagerly at DI-registration time, before
        // WebApplicationFactory's ConfigureAppConfiguration overrides apply; an environment variable is
        // folded in by CreateBuilder before that read and outranks appsettings.Development.json.
        Environment.SetEnvironmentVariable("ConnectionStrings__candidates", _postgres.GetConnectionString() + ";SSL Mode=Disable");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");

            // Registering a candidate requires an admin (write) role. Trust a local HS256 test key
            // instead of Keycloak's JWKS so an admin token can be minted offline.
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
                    o.TokenValidationParameters.NameClaimType = "preferred_username";
                });
            });
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CandidatesDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__candidates", null);
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // A client carrying an admin (write) bearer token — required to POST a new candidate.
    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestToken.ForRoles(["admin.write"]));
        return client;
    }

    [Fact]
    public async Task Register_then_get_round_trips_the_candidate()
    {
        var client = AdminClient();

        var register = await client.PostAsJsonAsync(
            "/v1/candidates",
            new RegisterCandidateCommand("Tariro", "Moyo", "Windhoek", "Namibian", "OpenToOpportunities", "Backend engineer"));

        register.StatusCode.Should().Be(HttpStatusCode.Created);
        register.Headers.Location.Should().NotBeNull();

        var created = await register.Content.ReadFromJsonAsync<CandidateDto>();
        created.Should().NotBeNull();
        created!.FirstName.Should().Be("Tariro");

        var fetch = await client.GetAsync(register.Headers.Location);
        fetch.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await fetch.Content.ReadFromJsonAsync<CandidateDto>();
        fetched!.Id.Should().Be(created.Id);
        fetched.City.Should().Be("Windhoek");
        fetched.Availability.Should().Be("OpenToOpportunities");
    }

    [Fact]
    public async Task Register_writes_a_message_to_the_transactional_outbox()
    {
        var client = AdminClient();

        var register = await client.PostAsJsonAsync(
            "/v1/candidates",
            new RegisterCandidateCommand("Anesu", "Chikore", "Harare", "Zimbabwean"));
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CandidatesDbContext>();
        // The integration event is written to the MassTransit outbox table in the same transaction
        // as the aggregate; with no broker running in the test it stays there (count > 0).
        var outboxCount = (await db.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM candidates.\"OutboxMessage\"")
            .ToListAsync()).Single();

        outboxCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Get_unknown_id_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/v1/candidates/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Register_with_invalid_availability_returns_400()
    {
        var client = AdminClient();

        var response = await client.PostAsJsonAsync(
            "/v1/candidates",
            new RegisterCandidateCommand("Tariro", "Moyo", "Windhoek", "Namibian", "Whenever"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
