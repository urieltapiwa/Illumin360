using FluentAssertions;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class TalentPoolsTests
{
    [Fact]
    public async Task Create_requires_a_name()
    {
        var handler = new CreateTalentPoolCommandHandler(Substitute.For<ICandidateRepository>());
        var result = await handler.HandleAsync(new CreateTalentPoolCommand("  "), CancellationToken.None);
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Create_persists_the_pool()
    {
        var repo = Substitute.For<ICandidateRepository>();
        var handler = new CreateTalentPoolCommandHandler(repo);
        var result = await handler.HandleAsync(new CreateTalentPoolCommand("Backend stars"), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Backend stars");
        repo.Received(1).AddPool(Arg.Any<TalentPool>());
    }

    [Fact]
    public async Task AddToPool_conflicts_when_already_a_member()
    {
        var pool = TalentPool.Create("P", DateTimeOffset.UnixEpoch).Value!;
        var candidate = Candidate.Register("A", "B", "Windhoek", "Namibian").Value!;
        var repo = Substitute.For<ICandidateRepository>();
        repo.GetPoolAsync(Arg.Any<TalentPoolId>(), Arg.Any<CancellationToken>()).Returns(pool);
        repo.GetByIdAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns(candidate);
        repo.GetPoolMemberAsync(Arg.Any<TalentPoolId>(), Arg.Any<CandidateId>(), Arg.Any<CancellationToken>())
            .Returns(new TalentPoolMember(Guid.NewGuid(), pool.Id, candidate.Id, DateTimeOffset.UnixEpoch));

        var result = await new AddToPoolCommandHandler(repo).HandleAsync(
            new AddToPoolCommand(pool.Id.Value, candidate.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        repo.DidNotReceiveWithAnyArgs().AddPoolMember(default!);
    }

    [Fact]
    public async Task AddToPool_adds_a_new_member()
    {
        var pool = TalentPool.Create("P", DateTimeOffset.UnixEpoch).Value!;
        var candidate = Candidate.Register("A", "B", "Windhoek", "Namibian").Value!;
        var repo = Substitute.For<ICandidateRepository>();
        repo.GetPoolAsync(Arg.Any<TalentPoolId>(), Arg.Any<CancellationToken>()).Returns(pool);
        repo.GetByIdAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns(candidate);
        repo.GetPoolMemberAsync(Arg.Any<TalentPoolId>(), Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns((TalentPoolMember?)null);

        var result = await new AddToPoolCommandHandler(repo).HandleAsync(
            new AddToPoolCommand(pool.Id.Value, candidate.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repo.Received(1).AddPoolMember(Arg.Any<TalentPoolMember>());
    }
}
