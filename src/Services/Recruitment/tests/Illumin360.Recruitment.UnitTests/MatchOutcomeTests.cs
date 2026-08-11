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
    public void Capture_snapshots_recruitment_features()
    {
        var o = MatchOutcome.Capture(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "professional", 70m, true, DateTimeOffset.UnixEpoch, "Referral", remote: true, interviewCount: 3, avgInterviewRating: 4.5m, hadOffer: true, daysToDecision: 12).Value!;
        o.Source.Should().Be("referral"); // normalised
        o.Remote.Should().BeTrue();
        o.InterviewCount.Should().Be(3);
        o.AvgInterviewRating.Should().Be(4.5m);
        o.HadOffer.Should().BeTrue();
        o.DaysToDecision.Should().Be(12);
    }

    [Fact]
    public async Task Reject_captures_a_rejection_outcome_once_with_features()
    {
        var app = RecruitmentApplication.Apply(RequestId.New(), Guid.NewGuid(), "professional", DateTimeOffset.UnixEpoch);
        var repo = Substitute.For<IRecruitmentRepository>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        repo.GetApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>()).Returns(app);
        repo.GetMatchOutcomeAsync(app.Id.Value, Arg.Any<CancellationToken>()).Returns((MatchOutcome?)null);
        repo.GetOutcomeFeaturesAsync(app.Id.Value, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new OutcomeFeatureSnapshot("careers", true, 2, 3.5m, true));
        var handler = new RejectApplicationCommandHandler(repo, publisher);

        var result = await handler.HandleAsync(new RejectApplicationCommand(app.Id.Value, "Not a fit", "Recruiter"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repo.Received(1).AddMatchOutcome(Arg.Is<MatchOutcome>(o => o.Outcome == "rejected" && o.Source == "careers" && o.Remote && o.InterviewCount == 2 && o.HadOffer));
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

    [Fact]
    public void Csv_export_has_header_and_label_last()
    {
        var rows = new[]
        {
            MatchOutcome.Capture(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "professional", 88m, true, DateTimeOffset.UnixEpoch, "careers", true, 2, 4m, true, 9).Value!,
        };

        var csv = OutcomesCsv.Render(rows);

        var lines = csv.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n').Split('\n');
        lines[0].Should().StartWith("application_id,").And.EndWith(",hired"); // features first, label last
        lines[1].Should().Contain(",careers,1,2,4,1,").And.EndWith(",1");     // remote=1, hadOffer=1, hired=1
    }
}
