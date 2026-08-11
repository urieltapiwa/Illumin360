using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using NSubstitute;
using Xunit;
using ApplicationId = Illumin360.Recruitment.Domain.ApplicationId;

namespace Illumin360.Recruitment.UnitTests;

public class MatchOutcomeTests
{
    [Fact]
    public void Capture_labels_and_clamps()
    {
        var ok = MatchOutcome.Capture(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Professional", 140m, hired: true, DateTimeOffset.UnixEpoch);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.Outcome.Should().Be("hired");
        ok.Value!.IsHire.Should().BeTrue();
        ok.Value!.MatchScore.Should().Be(100m);   // clamped
        ok.Value!.TalentType.Should().Be("professional"); // normalised

        MatchOutcome.Capture(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "professional", 50m, false, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Reject_captures_a_rejection_outcome_once()
    {
        var app = RecruitmentApplication.Apply(RequestId.New(), Guid.NewGuid(), "professional", DateTimeOffset.UnixEpoch);
        var repo = Substitute.For<IRecruitmentRepository>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        repo.GetApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>()).Returns(app);
        repo.GetMatchOutcomeAsync(app.Id.Value, Arg.Any<CancellationToken>()).Returns((MatchOutcome?)null);
        var handler = new RejectApplicationCommandHandler(repo, publisher);

        var result = await handler.HandleAsync(new RejectApplicationCommand(app.Id.Value, "Not a fit", "Recruiter"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repo.Received(1).AddMatchOutcome(Arg.Is<MatchOutcome>(o => o.Outcome == "rejected" && o.ApplicationId == app.Id.Value));
    }

    [Fact]
    public async Task Reject_does_not_double_capture_when_outcome_exists()
    {
        var app = RecruitmentApplication.Apply(RequestId.New(), Guid.NewGuid(), "professional", DateTimeOffset.UnixEpoch);
        var repo = Substitute.For<IRecruitmentRepository>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        repo.GetApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>()).Returns(app);
        repo.GetMatchOutcomeAsync(app.Id.Value, Arg.Any<CancellationToken>())
            .Returns(MatchOutcome.Capture(app.Id.Value, Guid.NewGuid(), app.TalentId, "professional", 0m, false, DateTimeOffset.UnixEpoch).Value!);
        var handler = new RejectApplicationCommandHandler(repo, publisher);

        await handler.HandleAsync(new RejectApplicationCommand(app.Id.Value), CancellationToken.None);

        repo.DidNotReceive().AddMatchOutcome(Arg.Any<MatchOutcome>());
    }

    [Fact]
    public async Task Summary_reports_counts_and_avg_score_by_outcome()
    {
        var req = Guid.NewGuid();
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.ListMatchOutcomesAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            MatchOutcome.Capture(Guid.NewGuid(), req, Guid.NewGuid(), "professional", 80m, true, DateTimeOffset.UnixEpoch).Value!,
            MatchOutcome.Capture(Guid.NewGuid(), req, Guid.NewGuid(), "professional", 90m, true, DateTimeOffset.UnixEpoch).Value!,
            MatchOutcome.Capture(Guid.NewGuid(), req, Guid.NewGuid(), "student", 40m, false, DateTimeOffset.UnixEpoch).Value!,
        });
        var handler = new GetMatchOutcomesQueryHandler(repo);

        var result = await handler.HandleAsync(new GetMatchOutcomesQuery(), CancellationToken.None);

        result.Value!.Total.Should().Be(3);
        result.Value!.Hired.Should().Be(2);
        result.Value!.Rejected.Should().Be(1);
        result.Value!.AvgScoreHired.Should().Be(85.0);   // (80+90)/2
        result.Value!.AvgScoreRejected.Should().Be(40.0);
    }
}
