using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Illumin360.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Illumin360.Employers.IntegrationTests;

/// <summary>
/// Exercises the employer team-member endpoints against a real PostgreSQL via Testcontainers. The demo
/// employer is seeded with one owner, so these tests can assert the "at least one owner" invariant.
/// </summary>
public sealed class TeamApiTests : IAsyncLifetime
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

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.ForRoles(["admin.write"]));
        return client;
    }

    [Fact]
    public async Task Seeded_team_contains_the_founding_owner()
    {
        var client = _factory.CreateClient();
        using var doc = JsonDocument.Parse(await client.GetStringAsync("/v1/employers/me/team"));
        var members = doc.RootElement.EnumerateArray().ToList();
        members.Should().HaveCount(1);
        members[0].GetProperty("role").GetString().Should().Be("owner");
        members[0].GetProperty("email").GetString().Should().Be("owner@namibmills.com.na");
    }

    [Fact]
    public async Task Invite_requires_admin_token()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/v1/employers/me/team", new { email = "x@acme.na", displayName = "X", role = "viewer" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invite_then_change_role_then_remove_roundtrips()
    {
        var client = AdminClient();

        var invite = await client.PostAsJsonAsync("/v1/employers/me/team", new { email = "recruiter@acme.na", displayName = "Rita Recruiter", role = "recruiter" });
        invite.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await invite.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var promote = await client.PutAsJsonAsync($"/v1/employers/me/team/{id}/role", new { role = "owner" });
        promote.StatusCode.Should().Be(HttpStatusCode.OK);
        (await promote.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("role").GetString().Should().Be("owner");

        var remove = await client.DeleteAsync($"/v1/employers/me/team/{id}");
        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Duplicate_email_invite_conflicts()
    {
        var client = AdminClient();
        var first = await client.PostAsJsonAsync("/v1/employers/me/team", new { email = "dupe@acme.na", displayName = "First", role = "viewer" });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/v1/employers/me/team", new { email = "dupe@acme.na", displayName = "Second", role = "viewer" });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Removing_the_last_owner_is_blocked()
    {
        var client = AdminClient();
        using var doc = JsonDocument.Parse(await client.GetStringAsync("/v1/employers/me/team"));
        var ownerId = doc.RootElement.EnumerateArray().First(m => m.GetProperty("role").GetString() == "owner").GetProperty("id").GetGuid();

        var remove = await client.DeleteAsync($"/v1/employers/me/team/{ownerId}");
        remove.StatusCode.Should().Be(HttpStatusCode.Conflict);
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
