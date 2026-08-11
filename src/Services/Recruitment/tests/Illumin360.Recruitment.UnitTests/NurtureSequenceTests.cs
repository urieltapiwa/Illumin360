using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.Recruitment.IntegrationEvents;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class NurtureSequenceTests
{
    private static readonly Guid SeqId = Guid.NewGuid();

    private static NurtureStep Step(int order, int delay)
        => NurtureStep.Create(SeqId, order, delay, $"Subject {order}", $"Body {order}", DateTimeOffset.UnixEpoch).Value!;

    [Fact]
    public async Task Enroll_requires_at_least_one_step()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetNurtureSequenceAsync(SeqId, Arg.Any<CancellationToken>()).Returns(NurtureSequence.Create("Talent warm-up", DateTimeOffset.UnixEpoch).Value!);
        repo.ListNurtureStepsAsync(SeqId, Arg.Any<CancellationToken>()).Returns([]);
        var handler = new EnrollRecipientCommandHandler(repo);

        var result = await handler.HandleAsync(new EnrollRecipientCommand(SeqId, "a@b.com", "Aria"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("nurture.no_steps");
    }

    [Fact]
    public async Task Enroll_schedules_the_first_step_and_dedupes()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetNurtureSequenceAsync(SeqId, Arg.Any<CancellationToken>()).Returns(NurtureSequence.Create("Warm-up", DateTimeOffset.UnixEpoch).Value!);
        repo.ListNurtureStepsAsync(SeqId, Arg.Any<CancellationToken>()).Returns(new[] { Step(1, 0), Step(2, 3) });
        repo.IsEnrolledAsync(SeqId, "a@b.com", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new EnrollRecipientCommandHandler(repo);

        var ok = await handler.HandleAsync(new EnrollRecipientCommand(SeqId, "a@b.com", "Aria"), CancellationToken.None);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.NextStepOrder.Should().Be(1);
        repo.Received(1).AddNurtureEnrollment(Arg.Any<NurtureEnrollment>());

        repo.IsEnrolledAsync(SeqId, "a@b.com", Arg.Any<CancellationToken>()).Returns(true);
        var dupe = await handler.HandleAsync(new EnrollRecipientCommand(SeqId, "a@b.com", "Aria"), CancellationToken.None);
        dupe.IsSuccess.Should().BeFalse();
        dupe.Error!.Code.Should().Be("nurture.already_enrolled");
    }

    [Fact]
    public async Task Runner_sends_the_current_step_and_advances_to_the_next()
    {
        var enrollment = NurtureEnrollment.Enroll(SeqId, "a@b.com", "Aria", 1, 0, DateTimeOffset.UnixEpoch).Value!;
        var repo = Substitute.For<IRecruitmentRepository>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        repo.ListDueEnrollmentsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(new[] { enrollment });
        repo.ListNurtureStepsAsync(SeqId, Arg.Any<CancellationToken>()).Returns(new[] { Step(1, 0), Step(2, 3) });
        var runner = new NurtureRunner(repo, publisher);

        var now = DateTimeOffset.UnixEpoch.AddDays(1);
        var sent = await runner.RunOnceAsync(now, CancellationToken.None);

        sent.Should().Be(1);
        await publisher.Received(1).PublishAsync(Arg.Is<CampaignEmailRequested>(e => e.To == "a@b.com" && e.Subject == "Subject 1"), Arg.Any<CancellationToken>());
        enrollment.NextStepOrder.Should().Be(2);
        enrollment.NextSendAt.Should().Be(now.AddDays(3));
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
    }

    [Fact]
    public async Task Runner_completes_the_enrollment_after_the_last_step()
    {
        var enrollment = NurtureEnrollment.Enroll(SeqId, "a@b.com", "Aria", 2, 0, DateTimeOffset.UnixEpoch).Value!;
        var repo = Substitute.For<IRecruitmentRepository>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        repo.ListDueEnrollmentsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(new[] { enrollment });
        repo.ListNurtureStepsAsync(SeqId, Arg.Any<CancellationToken>()).Returns(new[] { Step(1, 0), Step(2, 3) });
        var runner = new NurtureRunner(repo, publisher);

        var sent = await runner.RunOnceAsync(DateTimeOffset.UnixEpoch.AddDays(5), CancellationToken.None);

        sent.Should().Be(1);
        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
    }

    [Fact]
    public async Task Stopped_enrollment_is_not_re_stopped()
    {
        var enrollment = NurtureEnrollment.Enroll(SeqId, "a@b.com", null, 1, 0, DateTimeOffset.UnixEpoch).Value!;
        enrollment.Stop(DateTimeOffset.UnixEpoch);
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetNurtureEnrollmentAsync(enrollment.Id, Arg.Any<CancellationToken>()).Returns(enrollment);
        var handler = new StopEnrollmentCommandHandler(repo);

        var result = await handler.HandleAsync(new StopEnrollmentCommand(enrollment.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("nurture.enrollment_not_active");
    }
}
