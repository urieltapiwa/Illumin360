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

namespace Illumin360.Employers.IntegrationTests;

/// <summary>
/// Boots the Employers service against a real PostgreSQL via Testcontainers (migrate + seed on startup),
/// and reconfigures JWT bearer with the shared test key. Requires a Docker daemon on the test host.
/// </summary>
public sealed class EmployersApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("illumin360_employers")
        .WithUsername("illumin")
        .WithPassword("illumin_dev_pw")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await WaitForPostgresAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__employers", _postgres.GetConnectionString() + ";SSL Mode=Disable");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseTestAuth();
        });
        _ = _factory.Server;
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__employers", null);
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Seeded_demo_employer_is_returned_from_me()
    {
        var client = _factory.CreateClient();
        using var doc = JsonDocument.Parse(await client.GetStringAsync("/v1/employers/me"));
        doc.RootElement.GetProperty("companyName").GetString().Should().Be("Namib Mills");
    }

    [Fact]
    public async Task Register_without_token_returns_401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/v1/employers", new { companyName = "Acme", industry = "Tech", city = "Windhoek" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_with_admin_token_returns_201()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.ForRoles(["admin.write"]));
        var response = await client.PostAsJsonAsync("/v1/employers", new { companyName = "Acme", industry = "Tech", city = "Windhoek" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task WaitForPostgresAsync()
    {
        var cs = _postgres.GetConnectionString() + ";SSL Mode=Disable";
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var c = new NpgsqlConnection(cs);
                await c.OpenAsync();
                return;
            }
            catch (NpgsqlException) when (attempt < 20)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }
    }
}
