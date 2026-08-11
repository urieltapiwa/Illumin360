using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class RankedApplicationsTests
{
    private static readonly Guid RequestId = Guid.NewGuid();

    private static MatchOutcome Outcome(decimal score, bool hired, int interviews, decimal? rating, bool offer)
        => MatchOutcome.Capture(Guid.NewGuid(), Guid.NewGuid(), RequestId, "professional", score, hired, DateTimeOffset.UnixEpoch, "careers", false, interviews, rating, offer, 5).Value!;

    // A training set where matchScore alone (the baseline) does NOT separate classes — both sit around 50 —
    // but interviews + rating + offer do. So the learned model can beat the match-score heuristic.
    private static List<MatchOutcome> SeparableOutcomes()
    {
        var rows = new List<MatchOutcome>();
        for (var i = 0; i < 30; i++)
        {
            var hire = i % 2 == 0;
            rows.Add(hire
                ? Outcome(48 + (i % 6), true, 3, 4.5m, true)
                : Outcome(50 + (i % 6), false, 1, 2m, false));
        }

        return rows;
    }

    // Apply() forces MatchScore = 0 (that field is externally seeded in production). For a realistic ranking
    // test we set an in-distribution score via reflection so it doesn't dwarf the pipeline features.
    private static RecruitmentApplication App(decimal matchScore = 50m)
    {
        var app = RecruitmentApplication.Apply(new RequestId(RequestId), Guid.NewGuid(), "professional", DateTimeOffset.UnixEpoch);
        typeof(RecruitmentApplication).GetProperty(nameof(RecruitmentApplication.MatchScore))!
            .SetValue(app, matchScore);
        return app;
    }

    [Fact]
    public async Task Falls_back_to_heuristic_when_flag_off()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        var apps = new[] { App(), App() };
        repo.ListApplicationsAsync(Arg.Any<RequestId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(apps);
        var handler = new GetRankedApplicationsQueryHandler(repo);

        var result = await handler.HandleAsync(new GetRankedApplicationsQuery(RequestId, false), CancellationToken.None);

        result.Value!.UsedModel.Should().BeFalse();
        result.Value!.Applications.Should().HaveCount(2);
        await repo.DidNotReceive().ListMatchOutcomesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Falls_back_to_heuristic_when_not_enough_labelled_data()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.ListApplicationsAsync(Arg.Any<RequestId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new[] { App() });
        repo.ListMatchOutcomesAsync(Arg.Any<CancellationToken>()).Returns(new[] { Outcome(80, true, 3, 4.5m, true), Outcome(20, false, 1, 2m, false) });
        var handler = new GetRankedApplicationsQueryHandler(repo);

        var result = await handler.HandleAsync(new GetRankedApplicationsQuery(RequestId, true), CancellationToken.None);

        result.Value!.UsedModel.Should().BeFalse();
        result.Value!.Message.Should().Contain("Not enough");
    }

    [Fact]
    public async Task Uses_learned_model_and_reorders_by_hire_likelihood_when_it_beats_the_heuristic()
    {
        var strong = App();
        var weak = App();
        var repo = Substitute.For<IRecruitmentRepository>();

        // Weak app first in input order — a learned reorder must surface the strong one.
        repo.ListApplicationsAsync(Arg.Any<RequestId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new[] { weak, strong });
        repo.ListMatchOutcomesAsync(Arg.Any<CancellationToken>()).Returns(SeparableOutcomes());

        // Strong applicant looks like the hires; weak looks like the rejections.
        repo.GetOutcomeFeaturesAsync(strong.Id.Value, RequestId, Arg.Any<CancellationToken>())
            .Returns(new OutcomeFeatureSnapshot("careers", false, 3, 4.5m, true, 90, 90, 90));
        repo.GetOutcomeFeaturesAsync(weak.Id.Value, RequestId, Arg.Any<CancellationToken>())
            .Returns(new OutcomeFeatureSnapshot("careers", false, 1, 2m, false, 10, 10, 10));

        var handler = new GetRankedApplicationsQueryHandler(repo);

        var result = await handler.HandleAsync(new GetRankedApplicationsQuery(RequestId, true), CancellationToken.None);

        result.Value!.UsedModel.Should().BeTrue();
        result.Value!.Applications[0].ApplicationId.Should().Be(strong.Id.Value);
        result.Value!.Applications[0].LearnedScore.Should().BeGreaterThan(result.Value!.Applications[1].LearnedScore);
    }
}
