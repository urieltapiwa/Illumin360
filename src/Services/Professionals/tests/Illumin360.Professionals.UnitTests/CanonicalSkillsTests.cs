using FluentAssertions;
using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Application.Professionals;
using Illumin360.Professionals.Domain;
using NSubstitute;
using Xunit;

namespace Illumin360.Professionals.UnitTests;

public class CanonicalSkillsTests
{
    private static ProfessionalSkill Skill(ProfessionalId owner, string name)
        => new(Guid.NewGuid(), owner, name, 60, "steady", 0);

    [Fact]
    public async Task Maps_skills_to_canonical_and_flags_duplicates()
    {
        var me = Professional.Register("Panduleni", "Amukwa", "Developer", "Windhoek", "Namibian", "Open", "Builder").Value!;
        var repo = Substitute.For<IProfessionalRepository>();
        repo.GetDefaultProfessionalIdAsync(Arg.Any<CancellationToken>()).Returns(me.Id);
        repo.ListSkillsAsync(me.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { Skill(me.Id, "JS"), Skill(me.Id, "JavaScript"), Skill(me.Id, "Python") });

        var handler = new GetCanonicalSkillsQueryHandler(repo);
        var result = await handler.HandleAsync(new GetCanonicalSkillsQuery(), CancellationToken.None);

        var dto = result.Value!;
        dto.Skills.Should().HaveCount(3);
        dto.Skills.Single(s => s.Raw == "JS").CanonicalDisplay.Should().Be("JavaScript");
        dto.Skills.Single(s => s.Raw == "JS").Aliased.Should().BeTrue();
        dto.Skills.Single(s => s.Raw == "Python").Aliased.Should().BeFalse();

        dto.Duplicates.Should().ContainSingle();
        dto.Duplicates[0].CanonicalDisplay.Should().Be("JavaScript");
        dto.Duplicates[0].Members.Should().BeEquivalentTo("JS", "JavaScript");
    }

    [Fact]
    public async Task No_professional_yields_empty()
    {
        var repo = Substitute.For<IProfessionalRepository>();
        repo.GetDefaultProfessionalIdAsync(Arg.Any<CancellationToken>()).Returns((ProfessionalId?)null);

        var handler = new GetCanonicalSkillsQueryHandler(repo);
        var result = await handler.HandleAsync(new GetCanonicalSkillsQuery(), CancellationToken.None);

        result.Value!.Skills.Should().BeEmpty();
        result.Value!.Duplicates.Should().BeEmpty();
    }
}
