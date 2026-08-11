using FluentAssertions;
using Illumin360.Matching;
using Xunit;

namespace Illumin360.Matching.UnitTests;

public class ExplainTests
{
    [Fact]
    public void Explain_score_matches_Score()
    {
        var talent = new TalentProfile("Windhoek", "Senior Developer", ["C#", "PostgreSQL"], Seniority: "senior");
        var role = new RoleListing("Senior Developer", "Windhoek", "Technology", Seniority: "senior");

        MatchScorer.Explain(talent, role).Score.Should().Be(MatchScorer.Score(talent, role));
    }

    [Fact]
    public void Base_match_lists_three_signals_with_reasons()
    {
        var explanation = MatchScorer.Explain(
            new TalentProfile("Windhoek", "Software Developer", ["C#"]),
            new RoleListing("Software Developer", "Windhoek", "Technology"));

        explanation.Signals.Select(s => s.Name).Should().Equal("City", "Role", "Skills");
        explanation.Signals.Single(s => s.Name == "City").Reason.Should().Contain("Same city");
        // Normalised weights sum to ~1.
        explanation.Signals.Sum(s => s.Weight).Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void Optional_signals_appear_only_when_data_present_and_carry_reasons()
    {
        var explanation = MatchScorer.Explain(
            new TalentProfile("Windhoek", "Developer", ["C#"], SalaryExpectation: 40000, Seniority: "senior"),
            new RoleListing("Senior Developer", "Windhoek", "Tech", SalaryMin: 30000, SalaryMax: 50000, Seniority: "senior"));

        explanation.Signals.Select(s => s.Name).Should().Contain(["Salary", "Seniority"]);
        explanation.Signals.Single(s => s.Name == "Salary").Reason.Should().Contain("within budget");
        explanation.Signals.Single(s => s.Name == "Seniority").Reason.Should().Contain("matches");
    }
}
