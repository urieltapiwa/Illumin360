using FluentAssertions;
using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Application.Professionals;
using Illumin360.Professionals.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Professionals.UnitTests;

public class EditableSkillsTests
{
    private static readonly ProfessionalId Me = ProfessionalId.New();

    private static ProfessionalSkill Skill(string name, int level, int sort = 0)
        => new(Guid.NewGuid(), Me, name, level, "steady", sort);

    [Fact]
    public void UpdateLevel_clamps_range()
    {
        var s = Skill("C#", 50);
        s.UpdateLevel(90).IsSuccess.Should().BeTrue();
        s.Level.Should().Be(90);
        s.UpdateLevel(101).IsFailure.Should().BeTrue();
        s.UpdateLevel(-1).IsFailure.Should().BeTrue();
        s.Level.Should().Be(90); // unchanged on failure
    }

    [Fact]
    public async Task Add_rejects_blank_name_and_bad_level()
    {
        var repo = Substitute.For<IProfessionalRepository>();
        repo.GetDefaultProfessionalIdAsync(Arg.Any<CancellationToken>()).Returns(Me);
        var handler = new AddSkillCommandHandler(repo);

        (await handler.HandleAsync(new AddSkillCommand("  ", 50), CancellationToken.None)).IsFailure.Should().BeTrue();
        (await handler.HandleAsync(new AddSkillCommand("Go", 150), CancellationToken.None)).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Add_dedupes_by_name()
    {
        var repo = Substitute.For<IProfessionalRepository>();
        repo.GetDefaultProfessionalIdAsync(Arg.Any<CancellationToken>()).Returns(Me);
        repo.ListSkillsAsync(Me, Arg.Any<CancellationToken>()).Returns(new[] { Skill("C#", 70) });
        var handler = new AddSkillCommandHandler(repo);

        var result = await handler.HandleAsync(new AddSkillCommand("c#", 80), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        repo.DidNotReceive().AddSkill(Arg.Any<ProfessionalSkill>());
    }

    [Fact]
    public async Task Add_persists_new_skill()
    {
        var repo = Substitute.For<IProfessionalRepository>();
        repo.GetDefaultProfessionalIdAsync(Arg.Any<CancellationToken>()).Returns(Me);
        repo.ListSkillsAsync(Me, Arg.Any<CancellationToken>()).Returns(new[] { Skill("C#", 70, 0) });
        var handler = new AddSkillCommandHandler(repo);

        var result = await handler.HandleAsync(new AddSkillCommand("Rust", 65), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Rust");
        result.Value!.Level.Should().Be(65);
        repo.Received(1).AddSkill(Arg.Is<ProfessionalSkill>(s => s.Name == "Rust" && s.Sort == 1));
    }

    [Fact]
    public async Task Update_level_persists_when_skill_found()
    {
        var skill = Skill("C#", 50);
        var repo = Substitute.For<IProfessionalRepository>();
        repo.GetDefaultProfessionalIdAsync(Arg.Any<CancellationToken>()).Returns(Me);
        repo.GetSkillAsync(Me, skill.Id, Arg.Any<CancellationToken>()).Returns(skill);
        var handler = new UpdateSkillLevelCommandHandler(repo);

        var result = await handler.HandleAsync(new UpdateSkillLevelCommand(skill.Id, 88), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Level.Should().Be(88);
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Remove_missing_skill_returns_not_found()
    {
        var repo = Substitute.For<IProfessionalRepository>();
        repo.GetDefaultProfessionalIdAsync(Arg.Any<CancellationToken>()).Returns(Me);
        repo.GetSkillAsync(Me, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ProfessionalSkill?)null);
        var handler = new RemoveSkillCommandHandler(repo);

        var result = await handler.HandleAsync(new RemoveSkillCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}
