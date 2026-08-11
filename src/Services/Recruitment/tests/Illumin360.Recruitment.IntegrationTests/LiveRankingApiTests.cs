using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Testcontainers.PostgreSql;
using Xunit;

namespace Illumin360.Recruitment.IntegrationTests;

/// <summary>
/// End-to-end tests for the flag-gated live learned-ranking endpoint against the real HTTP pipeline
/// (routing → JWT bearer auth + AdminPolicy → the query handler → the real <c>Illumin360.Matching</c>
/// model → JSON), on a real PostgreSQL via Testcontainers so the host's startup migration boots.
/// The externally-owned recruitment_requests/applications tables are not created by migrations, so the
/// repository port is substituted with a deterministic seeded training set (the DB just lets the host start).
/// Requires a Docker daemon on the test host.
/// </summary>
public sealed class LiveRankingApiTests : IAsyncLifetime
{
    private static readonly Guid RequestId = Guid.NewGuid();

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("illumin360_recruitment")
        .WithUsername("illumin")
        .WithPassword("illumin_dev_pw")
        .Build();

    // The strong applicant looks like the hires (interviews, high rating, offer); the weak one like the
    // rejections. Both carry an in-distribution match score so it doesn't dwarf the pipeline features.
    private readonly RecruitmentApplication _strong = App();
    private readonly RecruitmentApplication _weak = App();

    public Task InitializeAsync() => _postgres.StartAsync();

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__recruitment", null);
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Ranked_endpoint_requires_authentication()
    {
        await using var factory = BuildFactory(flagEnabled: true);
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/v1/recruitment/requests/{RequestId}/applications/ranked");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Flag_off_returns_heuristic_order_without_the_model()
    {
        await using var factory = BuildFactory(flagEnabled: false);
        var client = factory.CreateClient();
        Authorize(client);

        var body = await client.GetFromJsonAsync<RankedResponse>($"/v1/recruitment/requests/{RequestId}/applications/ranked");

        body!.UsedModel.Should().BeFalse();
        body.Applications.Should().HaveCount(2);
    }

    [Fact]
    public async Task Flag_on_and_model_beats_heuristic_reorders_by_learned_score()
    {
        await using var factory = BuildFactory(flagEnabled: true);
        var client = factory.CreateClient();
        Authorize(client);

        var response = await client.GetAsync($"/v1/recruitment/requests/{RequestId}/applications/ranked");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = (await response.Content.ReadFromJsonAsync<RankedResponse>())!;
        body.UsedModel.Should().BeTrue();
        body.Applications[0].ApplicationId.Should().Be(_strong.Id.Value);
        body.Applications[0].LearnedScore.Should().BeGreaterThan(body.Applications[1].LearnedScore);
    }

    private WebApplicationFactory<Program> BuildFactory(bool flagEnabled)
    {
        // The host reads its connection string eagerly at DI-registration time (inside AddRecruitmentInfrastructure),
        // before ConfigureAppConfiguration overrides apply; an env var folded in by CreateBuilder outranks appsettings.
        Environment.SetEnvironmentVariable("ConnectionStrings__recruitment", _postgres.GetConnectionString() + ";SSL Mode=Disable");

        return new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("Matching:LearnedRankingEnabled", flagEnabled ? "true" : "false");
            b.UseSetting("JobAlerts:Enabled", "false");
            b.UseTestAuth();
            b.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRecruitmentRepository>();
                services.AddSingleton(BuildSeededRepository());
            });
        });
    }

    // A repository seeded with a separable training set (matchScore ~50 for both classes — so the
    // match-score baseline can't separate them, but interviews/rating/offer can — letting the learned
    // model beat the heuristic) plus two live applicants with contrasting feature snapshots.
    private IRecruitmentRepository BuildSeededRepository()
    {
        var repo = Substitute.For<IRecruitmentRepository>();

        var outcomes = new List<MatchOutcome>();
        for (var i = 0; i < 30; i++)
        {
            var hire = i % 2 == 0;
            outcomes.Add(hire
                ? Outcome(48 + (i % 6), true, 3, 4.5m, true)
                : Outcome(50 + (i % 6), false, 1, 2m, false));
        }

        repo.ListMatchOutcomesAsync(Arg.Any<CancellationToken>()).Returns(outcomes);
        repo.ListApplicationsAsync(Arg.Any<RequestId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { _weak, _strong });
        repo.GetOutcomeFeaturesAsync(_strong.Id.Value, RequestId, Arg.Any<CancellationToken>())
            .Returns(new OutcomeFeatureSnapshot("careers", false, 3, 4.5m, true, 90, 90, 90));
        repo.GetOutcomeFeaturesAsync(_weak.Id.Value, RequestId, Arg.Any<CancellationToken>())
            .Returns(new OutcomeFeatureSnapshot("careers", false, 1, 2m, false, 10, 10, 10));

        return repo;
    }

    private static MatchOutcome Outcome(decimal score, bool hired, int interviews, decimal? rating, bool offer)
        => MatchOutcome.Capture(Guid.NewGuid(), Guid.NewGuid(), RequestId, "professional", score, hired, DateTimeOffset.UnixEpoch, "careers", false, interviews, rating, offer, 5).Value!;

    // Apply() forces MatchScore = 0 (externally seeded in production); set an in-distribution score
    // via reflection so the ranking is driven by the pipeline features, not an out-of-range score.
    private static RecruitmentApplication App(decimal matchScore = 50m)
    {
        var app = RecruitmentApplication.Apply(new RequestId(RequestId), Guid.NewGuid(), "professional", DateTimeOffset.UnixEpoch);
        typeof(RecruitmentApplication).GetProperty(nameof(RecruitmentApplication.MatchScore))!.SetValue(app, matchScore);
        return app;
    }

    private static void Authorize(HttpClient client) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.ForRoles(["admin.superuser"]));

    private sealed record RankedResponse(bool UsedModel, string Message, IReadOnlyList<RankedItem> Applications);

    private sealed record RankedItem(Guid ApplicationId, string TalentType, string Status, decimal MatchScore, int LearnedScore);
}
