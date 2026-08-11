using FluentAssertions;
using Illumin360.Matching;
using Xunit;

namespace Illumin360.Matching.UnitTests;

public class SkillTaxonomyTests
{
    [Theory]
    [InlineData("JS", "javascript", "JavaScript")]
    [InlineData("  javascript ", "javascript", "JavaScript")]
    [InlineData("ECMAScript", "javascript", "JavaScript")]
    [InlineData("reactjs", "react", "React")]
    [InlineData("React.js", "react", "React")]
    [InlineData("c sharp", "csharp", "C#")]
    [InlineData("postgres", "postgresql", "PostgreSQL")]
    [InlineData("k8s", "kubernetes", "Kubernetes")]
    public void Canonicalizes_known_synonyms(string raw, string expectedId, string expectedDisplay)
    {
        var c = SkillTaxonomy.Canonicalize(raw);
        c.Id.Should().Be(expectedId);
        c.Display.Should().Be(expectedDisplay);
    }

    [Fact]
    public void Unknown_skills_pass_through_cleaned()
    {
        var c = SkillTaxonomy.Canonicalize("  Underwater   Basket Weaving ");
        c.Id.Should().Be("underwater-basket-weaving");
        c.Display.Should().Be("Underwater Basket Weaving");
    }

    [Fact]
    public void Blank_input_yields_empty_canonical()
    {
        SkillTaxonomy.Canonicalize("   ").Id.Should().BeEmpty();
        SkillTaxonomy.CanonicalId(null).Should().BeEmpty();
    }

    [Fact]
    public void Dedupe_collapses_synonyms_and_keeps_first_seen_order()
    {
        var result = SkillTaxonomy.Dedupe(["JS", "javascript", "React", "reactjs", "Python"]);

        result.Select(s => s.Display).Should().Equal("JavaScript", "React", "Python");
    }

    [Fact]
    public void DuplicateGroups_reports_synonym_clusters_only()
    {
        var groups = SkillTaxonomy.DuplicateGroups(["JS", "JavaScript", "Python", "reactjs", "React"]);

        groups.Should().HaveCount(2);
        groups.Should().Contain(g => g.Canonical.Display == "JavaScript" && g.Members.Count == 2);
        groups.Should().Contain(g => g.Canonical.Display == "React" && g.Members.Count == 2);
        groups.Should().NotContain(g => g.Canonical.Display == "Python");
    }
}
