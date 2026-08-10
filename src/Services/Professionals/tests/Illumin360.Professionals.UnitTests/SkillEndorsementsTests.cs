using FluentAssertions;
using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Application.Professionals;
using Illumin360.Professionals.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Professionals.UnitTests;

public class SkillEndorsementsTests
{
    private static readonly ProfessionalId Me = ProfessionalId.New();

    private static ProfessionalSkill Skill()
        => new(Guid.NewGuid(), Me, "C#", 70, "steady", 0);

    [Fact]
    public void Endorse_increments_count()
    {
        var s = Skill();
        s.Endorsements.Should().Be(0);
        s.Endorse();
        s.Endorse();
        s.Endorsements.Should().Be(2);
        s.Unendorse();
        s.Endorsements.Should().Be(1);
    }

    [Fact]
    public void Create_endorsement_validates_endorser()
    {
        SkillEndorsement.Create(Guid.NewGuid(), "  ", null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        SkillEndorsement.Create(Guid.Empty, "Jane", null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        SkillEndorsement.Create(Guid.NewGuid(), "Jane", "Worked with her", DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Endorse_missing_skill_is_not_found()
    {
        var repo = Substitute.For<IProfessionalRepository>();
        repo.GetSkillByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ProfessionalSkill?)null);
        var handler = new EndorseSkillCommandHandler(repo);

        var result = await handler.HandleAsync(new EndorseSkillCommand(Guid.NewGuid(), "Rita", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Duplicate_endorser_conflicts()
    {
        var skill = Skill();
        var repo = Substitute.For<IProfessionalRepository>();
        repo.GetSkillByIdAsync(skill.Id, Arg.Any<CancellationToken>()).Returns(skill);
        repo.EndorsementExistsAsync(skill.Id, "Rita", Arg.Any<CancellationToken>()).Returns(true);
        var handler = new EndorseSkillCommandHandler(repo);

        var result = await handler.HandleAsync(new EndorseSkillCommand(skill.Id, "Rita", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        repo.DidNotReceive().AddEndorsement(Arg.Any<SkillEndorsement>());
    }

    [Fact]
    public async Task Endorse_persists_and_bumps_count()
    {
        var skill = Skill();
        var repo = Substitute.For<IProfessionalRepository>();
        repo.GetSkillByIdAsync(skill.Id, Arg.Any<CancellationToken>()).Returns(skill);
        repo.EndorsementExistsAsync(skill.Id, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = new EndorseSkillCommandHandler(repo);

        var result = await handler.HandleAsync(new EndorseSkillCommand(skill.Id, "Rita Recruiter", "Top 5% engineer"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Endorser.Should().Be("Rita Recruiter");
        skill.Endorsements.Should().Be(1);
        repo.Received(1).AddEndorsement(Arg.Any<SkillEndorsement>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
