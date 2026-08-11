using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class RankModelTests
{
    private static MatchOutcome Outcome(decimal score, bool hired, int interviews = 0, decimal? rating = null, bool offer = false)
        => MatchOutcome.Capture(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "professional", score, hired, DateTimeOffset.UnixEpoch, "careers", false, interviews, rating, offer, 5).Value!;

    [Fact]
    public async Task Not_trained_below_minimum_samples()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.ListMatchOutcomesAsync(Arg.Any<CancellationToken>()).Returns(new[] { Outcome(80, true), Outcome(20, false) });
        var handler = new GetRankModelQueryHandler(repo);

        var result = await handler.HandleAsync(new GetRankModelQuery(), CancellationToken.None);

        result.Value!.Trained.Should().BeFalse();
        result.Value!.Message.Should().Contain("at least");
    }

    [Fact]
    public async Task Not_trained_without_both_classes()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.ListMatchOutcomesAsync(Arg.Any<CancellationToken>()).Returns(Enumerable.Range(0, 25).Select(_ => Outcome(80, true)).ToList());
        var handler = new GetRankModelQueryHandler(repo);

        var result = await handler.HandleAsync(new GetRankModelQuery(), CancellationToken.None);

        result.Value!.Trained.Should().BeFalse();
        result.Value!.Message.Should().Contain("both");
    }

    [Fact]
    public async Task Trains_and_reports_metrics_and_weights_with_enough_data()
    {
        // 30 decisions where hires have high scores + offers/interviews, rejections the opposite.
        var rows = new List<MatchOutcome>();
        for (var i = 0; i < 30; i++)
        {
            var hire = i % 2 == 0;
            rows.Add(hire
                ? Outcome(75 + (i % 10), true, interviews: 3, rating: 4.5m, offer: true)
                : Outcome(20 + (i % 10), false, interviews: 1, rating: 2m, offer: false));
        }

        var repo = Substitute.For<IRecruitmentRepository>();
        repo.ListMatchOutcomesAsync(Arg.Any<CancellationToken>()).Returns(rows);
        var handler = new GetRankModelQueryHandler(repo);

        var result = await handler.HandleAsync(new GetRankModelQuery(), CancellationToken.None);

        result.Value!.Trained.Should().BeTrue();
        result.Value!.SampleCount.Should().Be(30);
        result.Value!.Hired.Should().Be(15);
        result.Value!.Rejected.Should().Be(15);
        result.Value!.ModelAuc.Should().BeInRange(0, 1);
        result.Value!.Weights.Should().HaveCount(OutcomeFeatures.Names.Count);
        result.Value!.Weights.Select(w => w.Feature).Should().Contain("matchScore");
    }
}
