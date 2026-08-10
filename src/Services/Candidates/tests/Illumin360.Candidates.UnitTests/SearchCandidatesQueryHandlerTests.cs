using FluentAssertions;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class SearchCandidatesQueryHandlerTests
{
    [Fact]
    public async Task Rejects_invalid_availability()
    {
        var repo = Substitute.For<ICandidateRepository>();
        var handler = new SearchCandidatesQueryHandler(repo);

        var result = await handler.HandleAsync(new SearchCandidatesQuery(null, "Whenever", null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error!.Code.Should().Be("candidate.availability_invalid");
    }

    [Fact]
    public async Task Parses_criteria_and_returns_results_with_facets()
    {
        var candidate = Candidate.Register("Ada", "Lovelace", "Windhoek", "Namibian", AvailabilityStatus.OpenToOpportunities, "Backend engineer").Value!;
        var repo = Substitute.For<ICandidateRepository>();
        repo.SearchAsync(Arg.Any<CandidateSearchCriteria>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<Candidate> { candidate }, 1));
        repo.GetCandidateFacetsAsync(Arg.Any<CandidateSearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns(new CandidateFacetsDto([new CountByLabel("Windhoek", 1)], [new CountByLabel("OpenToOpportunities", 1)]));
        var handler = new SearchCandidatesQueryHandler(repo);

        var result = await handler.HandleAsync(new SearchCandidatesQuery("Windhoek", "openToOpportunities", "engineer", true, 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
        result.Value!.Items.Should().ContainSingle(c => c.FirstName == "Ada");
        result.Value!.Facets.Cities.Should().ContainSingle(f => f.Label == "Windhoek");

        await repo.Received(1).SearchAsync(
            Arg.Is<CandidateSearchCriteria>(c => c.City == "Windhoek" && c.Availability == AvailabilityStatus.OpenToOpportunities && c.Query == "engineer" && c.HasCv == true),
            Arg.Is(0),
            Arg.Is(20),
            Arg.Any<CancellationToken>());
    }
}
