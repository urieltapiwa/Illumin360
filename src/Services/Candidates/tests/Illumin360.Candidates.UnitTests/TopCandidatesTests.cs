using FluentAssertions;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class TopCandidatesTests
{
    private static Candidate Make(string first, string city, string headline) =>
        Candidate.Register(first, "Test", city, "Namibian", AvailabilityStatus.ActivelyLooking, headline).Value!;

    [Fact]
    public async Task Ranks_bestFitFirst_andHonoursLimit()
    {
        var repo = Substitute.For<ICandidateRepository>();
        repo.ListAsync(null, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(
        [
            Make("Chef", "Walvis Bay", "Head Chef"),
            Make("Dev", "Windhoek", "Software Developer"),
            Make("Analyst", "Windhoek", "Data Analyst"),
        ]);
        var handler = new GetTopCandidatesQueryHandler(repo);

        var result = await handler.HandleAsync(
            new GetTopCandidatesQuery("Software Developer", "Windhoek", Limit: 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var ranked = result.Value!;
        ranked.Should().HaveCount(2);
        ranked[0].Name.Should().Be("Dev Test"); // exact city + role → top
        ranked.Should().BeInDescendingOrder(x => x.Score);
    }

    [Fact]
    public async Task MissingTitle_FailsValidation()
    {
        var repo = Substitute.For<ICandidateRepository>();
        var handler = new GetTopCandidatesQueryHandler(repo);

        var result = await handler.HandleAsync(new GetTopCandidatesQuery("  ", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }
}
