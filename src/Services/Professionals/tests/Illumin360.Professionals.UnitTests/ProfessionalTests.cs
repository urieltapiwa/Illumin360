using FluentAssertions;
using Illumin360.Professionals.Domain;
using Illumin360.SharedKernel;
using Xunit;

namespace Illumin360.Professionals.UnitTests;

public class ProfessionalTests
{
    [Fact]
    public void Register_WithValidInput_SucceedsAndRaisesProfessionalRegistered()
    {
        var result = Professional.Register(
            "Panduleni", "Amukwa", "Software Developer", "Windhoek", "Namibian", "Open to opportunities", "Full-stack dev");

        result.IsSuccess.Should().BeTrue();
        var professional = result.Value!;
        professional.FullName.Should().Be("Panduleni Amukwa");
        professional.ProfileStrength.Should().Be(0);
        professional.ViewsTrend.Should().BeEmpty();
        professional.DomainEvents.Should().ContainSingle(e => e is ProfessionalRegistered);
    }

    [Theory]
    [InlineData("", "A", "Dev", "Windhoek", "professional.first_name_required")]
    [InlineData("P", "", "Dev", "Windhoek", "professional.last_name_required")]
    [InlineData("P", "A", "", "Windhoek", "professional.role_required")]
    [InlineData("P", "A", "Dev", "", "professional.city_required")]
    public void Register_WithMissingRequiredField_FailsWithValidationError(
        string first, string last, string role, string city, string expectedCode)
    {
        var result = Professional.Register(first, last, role, city, "Namibian", "Open", "Headline");

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error!.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Seed_HydratesAllFieldsAndRaisesNoEvent()
    {
        var id = Guid.NewGuid();
        var professional = Professional.Seed(
            id,
            "Panduleni",
            "Amukwa",
            "Software Developer",
            "Windhoek",
            "Namibian",
            "Open to opportunities",
            "Full-stack developer",
            profileStrength: 86,
            percentile: 12,
            memberSince: "2019",
            profileViews: 164,
            viewsDelta: 24,
            matchOpportunities: 18,
            matchDelta: 12,
            activeApplications: 5,
            responseRate: 64,
            avgMatch: 91,
            interviews: 3,
            viewsTrend: [20, 40, 82],
            salaryRole: "Software Developer",
            salaryP25: 32000,
            salaryMedian: 46000,
            salaryP75: 64000,
            salaryYou: 52000,
            createdAt: DateTimeOffset.UnixEpoch);

        professional.Id.Value.Should().Be(id);
        professional.ProfileStrength.Should().Be(86);
        professional.SalaryMedian.Should().Be(46000);
        professional.ViewsTrend.Should().Equal(20, 40, 82);
        professional.DomainEvents.Should().BeEmpty();
    }
}
