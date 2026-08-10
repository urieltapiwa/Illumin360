using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class InterviewAttendeesTests
{
    private static Interview AnInterview()
        => Interview.Schedule(Guid.NewGuid(), new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero), 60, "Video call", DateTimeOffset.UnixEpoch).Value!;

    [Fact]
    public void Attendee_validates_name_and_email()
    {
        InterviewAttendee.Create(Guid.NewGuid(), "", null, null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        InterviewAttendee.Create(Guid.NewGuid(), "Jane", "not-an-email", null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();

        var ok = InterviewAttendee.Create(Guid.NewGuid(), "Jane Panel", "  Jane@Acme.NA ", "Hiring Manager", DateTimeOffset.UnixEpoch);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.Email.Should().Be("jane@acme.na");
        ok.Value!.Role.Should().Be("hiring manager");
    }

    [Fact]
    public async Task Add_to_missing_interview_is_not_found()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetInterviewAsync(Arg.Any<InterviewId>(), Arg.Any<CancellationToken>()).Returns((Interview?)null);
        var handler = new AddInterviewAttendeeCommandHandler(repo);

        var result = await handler.HandleAsync(new AddInterviewAttendeeCommand(Guid.NewGuid(), "Jane", null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        repo.DidNotReceive().AddInterviewAttendee(Arg.Any<InterviewAttendee>());
    }

    [Fact]
    public async Task Add_persists_attendee()
    {
        var interview = AnInterview();
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetInterviewAsync(Arg.Any<InterviewId>(), Arg.Any<CancellationToken>()).Returns(interview);
        var handler = new AddInterviewAttendeeCommandHandler(repo);

        var result = await handler.HandleAsync(new AddInterviewAttendeeCommand(interview.Id.Value, "Rita", "rita@acme.na", "interviewer"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Rita");
        repo.Received(1).AddInterviewAttendee(Arg.Any<InterviewAttendee>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Ics_lists_panel_as_attendees()
    {
        var interview = AnInterview();
        var panel = new[]
        {
            InterviewAttendee.Create(interview.Id.Value, "Rita Recruiter", "rita@acme.na", "interviewer", DateTimeOffset.UnixEpoch).Value!,
            InterviewAttendee.Create(interview.Id.Value, "No Email", null, "observer", DateTimeOffset.UnixEpoch).Value!,
        };

        var ics = Ics.Build(interview, panel);

        ics.Should().Contain("ATTENDEE;CN=Rita Recruiter:mailto:rita@acme.na");
        ics.Should().Contain("ATTENDEE;CN=No Email:invalid:nomail");
        Ics.Build(interview).Should().NotContain("ATTENDEE"); // none when no panel
    }
}
