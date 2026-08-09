using FluentAssertions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class RecruitmentRequestTests
{
    private static readonly Guid Company = Guid.Parse("ad85e06d-3da0-4396-9c2e-62af89043067");

    [Fact]
    public void Post_WithValidData_Succeeds_AndRaisesEvent()
    {
        var result = RecruitmentRequest.Post(Company, "Network Engineer", "Windhoek", 2);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Network Engineer");
        result.Value.Status.Should().Be(RequestStatus.Open);
        result.Value.DomainEvents.Should().ContainSingle(e => e is RequestPosted);
    }

    [Theory]
    [InlineData("", "Windhoek", 1)]
    [InlineData("Network Engineer", "", 1)]
    [InlineData("Network Engineer", "Windhoek", 0)]
    public void Post_WithInvalidData_FailsValidation(string title, string city, int positions)
    {
        var result = RecruitmentRequest.Post(Company, title, city, positions);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Post_WithEmptyCompany_FailsValidation()
    {
        var result = RecruitmentRequest.Post(Guid.Empty, "Network Engineer", "Windhoek", 1);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("request.company_required");
    }

    [Fact]
    public void MarkFilled_SetsStatusAndTimestamp()
    {
        var request = RecruitmentRequest.Post(Company, "Network Engineer", "Windhoek", 1).Value!;

        request.MarkFilled();

        request.Status.Should().Be(RequestStatus.Filled);
        request.FilledAt.Should().NotBeNull();
    }
}
