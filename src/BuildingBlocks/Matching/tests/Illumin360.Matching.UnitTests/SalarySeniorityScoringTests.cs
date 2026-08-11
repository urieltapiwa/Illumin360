using FluentAssertions;
using Illumin360.Matching;
using Xunit;

namespace Illumin360.Matching.UnitTests;

public class SalarySeniorityScoringTests
{
    [Theory]
    [InlineData("Senior Developer", 3)]
    [InlineData("junior analyst", 1)]
    [InlineData("Lead Engineer", 4)]
    [InlineData("Graduate Trainee", 0)]
    [InlineData("Software Developer", null)]
    [InlineData("", null)]
    public void SeniorityParser_resolves_rank_from_text(string text, int? expected)
        => SeniorityParser.Rank(text).Should().Be(expected);

    [Fact]
    public void Adding_salary_or_seniority_does_not_change_base_scores_when_absent()
    {
        // Same three-signal inputs, once via the base record and once with explicit nulls.
        var baseScore = MatchScorer.Score(
            new TalentProfile("Windhoek", "Software Developer", ["C#"]),
            new RoleListing("Software Developer", "Windhoek", "Technology"));
        var explicitNulls = MatchScorer.Score(
            new TalentProfile("Windhoek", "Software Developer", ["C#"], SalaryExpectation: null, Seniority: null),
            new RoleListing("Software Developer", "Windhoek", "Technology", SalaryMin: null, SalaryMax: null, Seniority: null));

        explicitNulls.Should().Be(baseScore);
    }

    [Fact]
    public void Salary_within_band_scores_higher_than_over_ceiling()
    {
        var role = new RoleListing("Developer", "Windhoek", "Tech", SalaryMin: 30000, SalaryMax: 50000);
        var affordable = MatchScorer.Score(new TalentProfile("Windhoek", "Developer", ["C#"], SalaryExpectation: 45000), role);
        var tooPricey = MatchScorer.Score(new TalentProfile("Windhoek", "Developer", ["C#"], SalaryExpectation: 90000), role);

        affordable.Should().BeGreaterThan(tooPricey);
    }

    [Fact]
    public void Matching_seniority_scores_higher_than_mismatched()
    {
        var role = new RoleListing("Senior Developer", "Windhoek", "Tech", Seniority: "senior");
        var senior = MatchScorer.Score(new TalentProfile("Windhoek", "Developer", ["C#"], Seniority: "senior"), role);
        var intern = MatchScorer.Score(new TalentProfile("Windhoek", "Developer", ["C#"], Seniority: "intern"), role);

        senior.Should().BeGreaterThan(intern);
    }
}
