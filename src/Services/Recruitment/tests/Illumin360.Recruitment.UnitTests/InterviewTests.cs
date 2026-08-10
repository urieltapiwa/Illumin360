using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class InterviewTests
{
    private static Interview Scheduled() =>
        Interview.Schedule(Guid.NewGuid(), new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero), 45, "Video call", DateTimeOffset.UnixEpoch).Value!;

    [Fact]
    public void Schedule_validates_duration_and_location()
    {
        Interview.Schedule(Guid.NewGuid(), DateTimeOffset.UtcNow, 0, "x", DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        Interview.Schedule(Guid.NewGuid(), DateTimeOffset.UtcNow, 30, "  ", DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Feedback_completes_and_rejects_out_of_range_and_double_completion()
    {
        var i = Scheduled();
        i.RecordFeedback(6, null).IsFailure.Should().BeTrue(); // out of range
        i.Status.Should().Be("scheduled");

        i.RecordFeedback(4, "Strong").IsSuccess.Should().BeTrue();
        i.Status.Should().Be("completed");
        i.FeedbackRating.Should().Be(4);

        i.RecordFeedback(5, "again").IsFailure.Should().BeTrue(); // not scheduled anymore
    }

    [Fact]
    public void Cancel_only_from_scheduled()
    {
        var i = Scheduled();
        i.Cancel().IsSuccess.Should().BeTrue();
        i.Status.Should().Be("cancelled");
        i.Cancel().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Ics_contains_event_with_utc_times()
    {
        var ics = Ics.Build(Scheduled());

        ics.Should().Contain("BEGIN:VEVENT");
        ics.Should().Contain("DTSTART:20260901T090000Z");
        ics.Should().Contain("DTEND:20260901T094500Z");
        ics.Should().Contain("LOCATION:Video call");
    }

    [Fact]
    public async Task ScheduleHandler_persists_and_returns_dto()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        var handler = new ScheduleInterviewCommandHandler(repo);

        var result = await handler.HandleAsync(
            new ScheduleInterviewCommand(Guid.NewGuid(), new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero), 30, "Windhoek office"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("scheduled");
        repo.Received(1).AddInterview(Arg.Any<Interview>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
