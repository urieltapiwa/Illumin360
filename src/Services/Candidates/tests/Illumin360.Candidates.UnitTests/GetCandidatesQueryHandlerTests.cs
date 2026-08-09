using FluentAssertions;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;
using NSubstitute;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class GetCandidatesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_MapsDomainToDtos_AndClampsPageSize()
    {
        var repo = Substitute.For<ICandidateRepository>();
        var candidate = Candidate.Register("Tariro", "Moyo", "Windhoek", "Namibian").Value!;
        repo.ListAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([candidate]);

        var handler = new GetCandidatesQueryHandler(repo);
        var result = await handler.HandleAsync(new GetCandidatesQuery(City: "Windhoek", Page: 1, PageSize: 999), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle().Which.City.Should().Be("Windhoek");
        // PageSize 999 must be clamped to 100.
        await repo.Received(1).ListAsync("Windhoek", 0, 100, Arg.Any<CancellationToken>());
    }
}
