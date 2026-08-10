using FluentAssertions;
using Illumin360.SharedKernel;
using Illumin360.Students.Domain;
using Xunit;

namespace Illumin360.Students.UnitTests;

public class StudentMatchActionsTests
{
    private static StudentMatch NewMatch() => new(
        Guid.NewGuid(), new StudentId(Guid.NewGuid()), "IT Intern", "Unity Group", "Walvis Bay", 95, 9000, 16000, "Internship", "1d", 0);

    [Fact]
    public void NewMatch_StartsInNewStatus()
    {
        NewMatch().Status.Should().Be(MatchStatus.New);
    }

    [Fact]
    public void Save_SetsSavedStatus()
    {
        var match = NewMatch();
        match.Save();
        match.Status.Should().Be(MatchStatus.Saved);
    }

    [Fact]
    public void Dismiss_SetsDismissedStatus()
    {
        var match = NewMatch();
        match.Dismiss();
        match.Status.Should().Be(MatchStatus.Dismissed);
    }

    [Fact]
    public void Apply_FirstTime_SucceedsAndSetsAppliedStatus()
    {
        var match = NewMatch();
        var result = match.Apply();

        result.IsSuccess.Should().BeTrue();
        match.Status.Should().Be(MatchStatus.Applied);
    }

    [Fact]
    public void Apply_Twice_ConflictsAndStaysApplied()
    {
        var match = NewMatch();
        match.Apply();

        var second = match.Apply();

        second.IsFailure.Should().BeTrue();
        second.Error!.Type.Should().Be(ErrorType.Conflict);
        second.Error!.Code.Should().Be("match.already_applied");
        match.Status.Should().Be(MatchStatus.Applied);
    }

    [Fact]
    public void RecordApplication_IncrementsApplicationsCount()
    {
        var student = Student.Register(
            "Selma", "Nghidinwa", "Computer Science", "NUST", "Final year", "2026", "Illumin Futures", "Windhoek").Value!;

        student.RecordApplication();

        student.ApplicationsCount.Should().Be(1);
    }

    [Fact]
    public void SetAvailability_UpdatesLabel_AndIgnoresBlank()
    {
        var student = Student.Register(
            "Selma", "Nghidinwa", "Computer Science", "NUST", "Final year", "2026", "Illumin Futures", "Windhoek").Value!;

        student.SetAvailability("Not looking");
        student.Availability.Should().Be("Not looking");

        student.SetAvailability("   ");
        student.Availability.Should().Be("Not looking");
    }
}
