using FluentAssertions;
using Illumin360.Matching;
using Xunit;

namespace Illumin360.Matching.UnitTests;

public class SkillGapAnalyzerTests
{
    [Fact]
    public void Splits_required_into_matched_and_missing_case_insensitively()
    {
        var result = SkillGapAnalyzer.Analyze(
            candidateSkills: ["C#", "postgresql", "Docker"],
            requiredSkills: ["c#", "PostgreSQL", "Kubernetes", "Go"]);

        result.Matched.Should().BeEquivalentTo("c#", "postgresql");
        result.Missing.Should().BeEquivalentTo("kubernetes", "go");
        result.Missing[0].Should().Be("kubernetes"); // required order preserved
        result.Extra.Should().BeEquivalentTo("docker");
        result.CoveragePercent.Should().Be(50); // 2 of 4
    }

    [Fact]
    public void Trims_and_dedupes_inputs()
    {
        var result = SkillGapAnalyzer.Analyze(
            candidateSkills: [" Go ", "go", "SQL"],
            requiredSkills: ["go", " go ", "sql"]);

        result.Missing.Should().BeEmpty();
        result.Matched.Should().BeEquivalentTo("go", "sql"); // required de-duped
        result.CoveragePercent.Should().Be(100);
    }

    [Fact]
    public void No_required_skills_is_full_coverage()
    {
        var result = SkillGapAnalyzer.Analyze(["anything"], []);
        result.CoveragePercent.Should().Be(100);
        result.Missing.Should().BeEmpty();
    }

    [Fact]
    public void No_candidate_skills_is_zero_coverage()
    {
        var result = SkillGapAnalyzer.Analyze(null, ["go", "sql"]);
        result.CoveragePercent.Should().Be(0);
        result.Missing.Should().BeEquivalentTo("go", "sql");
        result.Matched.Should().BeEmpty();
    }
}
