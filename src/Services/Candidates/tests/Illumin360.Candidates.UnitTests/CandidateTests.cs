using FluentAssertions;
using Illumin360.Candidates.Domain;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class CandidateTests
{
    [Fact]
    public void Register_WithValidData_Succeeds_AndRaisesEvent()
    {
        var result = Candidate.Register("Tariro", "Moyo", "Windhoek", "Namibian");

        result.IsSuccess.Should().BeTrue();
        result.Value!.FirstName.Should().Be("Tariro");
        result.Value.DomainEvents.Should().ContainSingle(e => e is CandidateRegistered);
    }

    [Theory]
    [InlineData("", "Moyo")]
    [InlineData("Tariro", "")]
    public void Register_WithMissingName_FailsValidation(string first, string last)
    {
        var result = Candidate.Register(first, last, "Windhoek", "Namibian");

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(Illumin360.SharedKernel.ErrorType.Validation);
    }

    [Fact]
    public void Register_WithOverlongHeadline_FailsValidation()
    {
        var headline = new string('x', 151);
        var result = Candidate.Register("Tariro", "Moyo", "Windhoek", "Namibian", publicHeadline: headline);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("candidate.headline_too_long");
    }
}
