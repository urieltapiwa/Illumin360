using FluentAssertions;
using Illumin360.SharedKernel;
using Illumin360.Students.Domain;
using Xunit;

namespace Illumin360.Students.UnitTests;

public class StudentTests
{
    [Fact]
    public void Register_WithValidInput_SucceedsAndRaisesStudentRegistered()
    {
        var result = Student.Register(
            "Selma", "Nghidinwa", "Computer Science", "NUST", "Final year", "2026", "Illumin Futures", "Windhoek");

        result.IsSuccess.Should().BeTrue();
        var student = result.Value!;
        student.FullName.Should().Be("Selma Nghidinwa");
        student.Readiness.Should().Be(0);
        student.ViewsTrend.Should().BeEmpty();
        student.DomainEvents.Should().ContainSingle(e => e is StudentRegistered);
    }

    [Theory]
    [InlineData("", "N", "CS", "Windhoek", "student.first_name_required")]
    [InlineData("S", "", "CS", "Windhoek", "student.last_name_required")]
    [InlineData("S", "N", "", "Windhoek", "student.field_required")]
    [InlineData("S", "N", "CS", "", "student.city_required")]
    public void Register_WithMissingRequiredField_FailsWithValidationError(
        string first, string last, string field, string city, string expectedCode)
    {
        var result = Student.Register(first, last, field, "NUST", "Final year", "2026", "Programme", city);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error!.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Seed_HydratesAllFieldsAndRaisesNoEvent()
    {
        var id = Guid.NewGuid();
        var student = Student.Seed(
            id,
            "Selma",
            "Nghidinwa",
            "Computer Science",
            "NUST",
            "Final year",
            "2026",
            "Illumin Futures",
            "Windhoek",
            readiness: 78,
            profileViews: 76,
            viewsDelta: 31,
            mentorSessions: 3,
            applicationsCount: 4,
            viewsTrend: [4, 6, 76],
            createdAt: DateTimeOffset.UnixEpoch);

        student.Id.Value.Should().Be(id);
        student.Readiness.Should().Be(78);
        student.ProfileViews.Should().Be(76);
        student.ViewsTrend.Should().Equal(4, 6, 76);
        student.DomainEvents.Should().BeEmpty();
    }
}
