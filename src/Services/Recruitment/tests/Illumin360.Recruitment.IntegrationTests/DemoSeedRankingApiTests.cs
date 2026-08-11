using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Illumin360.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Illumin360.Recruitment.IntegrationTests;

/// <summary>
/// Validates the shipped docker demo seed (deploy/docker/demo/seed-recruitment-demo.sql) end to end
/// against the REAL repository: boots the API on a Testcontainers PostgreSQL so the startup migration
/// creates the service-owned tables, runs the exact demo SQL, then calls the flag-gated live-ranking
/// endpoint and asserts the learned model beats the heuristic and re-orders the seeded applicants.
/// This is the same code path the docker-compose.demo.yml profile exercises in the browser.
/// Requires a Docker daemon on the test host.
/// </summary>
public sealed class DemoSeedRankingApiTests : IAsyncLifetime
{
    private static readonly Guid DemoRequestId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // Highest match score among the seeded applicants (Aria, cold pipeline) — the heuristic's #1.
    private static readonly Guid HighestMatchApplicant = Guid.Parse("a0000001-0000-0000-0000-000000000001");

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("illumin360_recruitment")
        .WithUsername("illumin")
        .WithPassword("illumin_dev_pw")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__recruitment", null);
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Demo_seed_makes_the_endpoint_rank_by_the_learned_model()
    {
        var connectionString = _postgres.GetConnectionString() + ";SSL Mode=Disable";
        Environment.SetEnvironmentVariable("ConnectionStrings__recruitment", connectionString);

        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("Matching:LearnedRankingEnabled", "true");
            b.UseSetting("JobAlerts:Enabled", "false");
            b.UseTestAuth();
        });

        // Force the host to build now so the startup migration creates the service-owned tables.
        _ = factory.Server;

        await RunDemoSeedAsync(connectionString);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.ForRoles(["admin.superuser"]));

        var response = await client.GetAsync($"/v1/recruitment/requests/{DemoRequestId}/applications/ranked");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = (await response.Content.ReadFromJsonAsync<RankedResponse>())!;

        // The model beat the heuristic on the seeded hold-out, so it drove the ranking.
        body.UsedModel.Should().BeTrue();
        body.Applications.Should().HaveCount(6);

        // The demo's whole point: the highest-match-but-cold applicant is NOT ranked first by the model.
        body.Applications[0].ApplicationId.Should().NotBe(HighestMatchApplicant);

        // And the input (heuristic) order really was highest-match-first, so this is a genuine re-order.
        body.Applications.MaxBy(a => a.MatchScore)!.ApplicationId.Should().Be(HighestMatchApplicant);

        // The top learned pick should out-score the highest-match applicant on hire-likelihood.
        var topLearned = body.Applications[0];
        var highestMatch = body.Applications.Single(a => a.ApplicationId == HighestMatchApplicant);
        topLearned.LearnedScore.Should().BeGreaterThan(highestMatch.LearnedScore);
    }

    private static async Task RunDemoSeedAsync(string connectionString)
    {
        var sql = await File.ReadAllTextAsync(LocateSeedFile());

        // Strip psql meta-commands (e.g. \set) that the raw Npgsql protocol can't execute.
        var lines = sql.Split('\n').Where(l => !l.TrimStart().StartsWith('\\'));
        var runnable = string.Join('\n', lines);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // The command text is a trusted static SQL file shipped in the repo, not user input.
#pragma warning disable CA2100
        await using var command = new NpgsqlCommand(runnable, connection);
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }

    private static string LocateSeedFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "deploy", "docker", "demo", "seed-recruitment-demo.sql");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("Could not locate deploy/docker/demo/seed-recruitment-demo.sql by walking up from the test output directory.");
    }

    private sealed record RankedResponse(bool UsedModel, string Message, IReadOnlyList<RankedItem> Applications);

    private sealed record RankedItem(Guid ApplicationId, string TalentType, string Status, decimal MatchScore, int LearnedScore);
}
