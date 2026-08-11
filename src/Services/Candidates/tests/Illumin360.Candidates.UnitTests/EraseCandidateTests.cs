using FluentAssertions;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class EraseCandidateTests
{
    [Fact]
    public async Task Missing_candidate_is_not_found_and_does_not_erase()
    {
        var repo = Substitute.For<ICandidateRepository>();
        repo.GetByIdAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns((Candidate?)null);
        var handler = new EraseCandidateCommandHandler(repo);

        var result = await handler.HandleAsync(new EraseCandidateCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await repo.DidNotReceive().EraseCandidateAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Existing_candidate_is_erased()
    {
        var candidate = Candidate.Register("Ada", "Lovelace", "Windhoek", "Namibian").Value!;
        var repo = Substitute.For<ICandidateRepository>();
        repo.GetByIdAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns(candidate);
        var handler = new EraseCandidateCommandHandler(repo);

        var result = await handler.HandleAsync(new EraseCandidateCommand(candidate.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).EraseCandidateAsync(candidate.Id, Arg.Any<CancellationToken>());
    }
}
