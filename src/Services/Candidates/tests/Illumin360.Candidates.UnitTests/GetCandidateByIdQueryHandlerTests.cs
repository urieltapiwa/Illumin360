using FluentAssertions;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class GetCandidateByIdQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenFound_ReturnsDto()
    {
        var repo = Substitute.For<ICandidateRepository>();
        var candidate = Candidate.Register("Tariro", "Moyo", "Windhoek", "Namibian").Value!;
        repo.GetByIdAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns(candidate);

        var handler = new GetCandidateByIdQueryHandler(repo);
        var result = await handler.HandleAsync(new GetCandidateByIdQuery(candidate.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(candidate.Id.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenMissing_ReturnsNotFound()
    {
        var repo = Substitute.For<ICandidateRepository>();
        repo.GetByIdAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns((Candidate?)null);

        var handler = new GetCandidateByIdQueryHandler(repo);
        var result = await handler.HandleAsync(new GetCandidateByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("candidate.not_found");
    }
}
