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
/// End-to-end test for talent rediscovery against the REAL repository: boots the API on a Testcontainers
/// PostgreSQL (startup migration creates the service-owned tables), seeds the externally-owned
/// recruitment_requests/applications plus a past rejected application + its captured outcome, then calls
/// GET …/rediscovery and asserts the silver-medalist surfaces. Exercises the repository's cross-table
/// query (external tables + strongly-typed keys + in-memory join) that unit tests can't cover.
/// Requires a Docker daemon on the test host.
/// </summary>
public sealed class RediscoveryApiTests : IAsyncLifetime
{
    private static readonly Guid TargetRequestId = Guid.Parse("d1111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherRequestId = Guid.Parse("d2222222-2222-2222-2222-222222222222");
    private static readonly Guid SilverMedalist = Guid.Parse("d3333333-3333-3333-3333-333333333333");

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
    public async Task Rediscovery_surfaces_a_past_not_hired_applicant_who_fits_the_target_role()
    {
        var connectionString = _postgres.GetConnectionString() + ";SSL Mode=Disable";
        Environment.SetEnvironmentVariable("ConnectionStrings__recruitment", connectionString);

        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("JobAlerts:Enabled", "false");
            b.UseTestAuth();
        });
        _ = factory.Server;

        await SeedAsync(connectionString);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.ForRoles(["admin.superuser"]));

        var response = await client.GetAsync($"/v1/recruitment/requests/{TargetRequestId}/rediscovery?take=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = (await response.Content.ReadFromJsonAsync<List<RediscoveredItem>>())!;
        items.Should().ContainSingle(i => i.TalentId == SilverMedalist);
        var found = items.Single(i => i.TalentId == SilverMedalist);
        found.PriorTitle.Should().Be("Software Engineer");
        found.HadOffer.Should().BeTrue();
        found.Score.Should().BeGreaterThan(50);
    }

    private static async Task SeedAsync(string connectionString)
    {
        var sql = $@"
CREATE SCHEMA IF NOT EXISTS recruitment;
CREATE TABLE IF NOT EXISTS recruitment.recruitment_requests (
    id uuid PRIMARY KEY, city varchar(100) NOT NULL, company_id uuid NOT NULL, created_at timestamptz NOT NULL,
    filled_at timestamptz NULL, positions integer NOT NULL, status varchar(20) NOT NULL, title varchar(150) NOT NULL);
CREATE TABLE IF NOT EXISTS recruitment.applications (
    id uuid PRIMARY KEY, applied_at timestamptz NOT NULL, decided_at timestamptz NULL, is_hire boolean NOT NULL,
    match_score numeric(5,2) NOT NULL, request_id uuid NOT NULL, status varchar(20) NOT NULL,
    talent_id uuid NOT NULL, talent_type varchar(20) NOT NULL);

INSERT INTO recruitment.recruitment_requests (id, city, company_id, created_at, positions, status, title) VALUES
    ('{TargetRequestId}', 'Windhoek', gen_random_uuid(), now() - interval '5 days', 1, 'open', 'Senior Software Engineer'),
    ('{OtherRequestId}',  'Windhoek', gen_random_uuid(), now() - interval '90 days', 1, 'filled', 'Software Engineer');

-- A past application to the OTHER role: rejected, but interviewed and offered.
INSERT INTO recruitment.applications (id, applied_at, is_hire, match_score, request_id, status, talent_id, talent_type)
VALUES ('d4444444-4444-4444-4444-444444444444', now() - interval '80 days', false, 78.00, '{OtherRequestId}', 'rejected', '{SilverMedalist}', 'professional');

INSERT INTO recruitment.match_outcomes
    (id, application_id, request_id, talent_id, talent_type, match_score, outcome, decided_at, source, remote,
     interview_count, avg_interview_rating, had_offer, days_to_decision, city_signal, role_signal, skill_signal)
VALUES (gen_random_uuid(), 'd4444444-4444-4444-4444-444444444444', '{OtherRequestId}', '{SilverMedalist}', 'professional',
     78.00, 'rejected', now() - interval '70 days', 'careers', false, 2, 4.00, true, 10, 80, 85, 82);
";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Static, trusted seed SQL — not user input.
#pragma warning disable CA2100
        await using var command = new NpgsqlCommand(sql, connection);
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }

    private sealed record RediscoveredItem(Guid TalentId, string TalentType, int Score, string Reason, string PriorTitle, string PriorStatus, decimal PriorMatchScore, int InterviewCount, bool HadOffer);
}
