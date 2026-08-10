using FluentAssertions;
using Illumin360.Matching;
using Xunit;

namespace Illumin360.Matching.UnitTests;

public class MatchScorerTests
{
    private static TalentProfile Talent(params string[] skills) =>
        new("Windhoek", "Software Developer", skills);

    [Fact]
    public void PerfectFit_SameCityRoleAndSkill_ScoresHigh()
    {
        var score = MatchScorer.Score(
            Talent("Python", "Databases"),
            new RoleListing("Software Developer", "Windhoek", "Technology"));

        score.Should().BeGreaterThanOrEqualTo(70);
    }

    [Fact]
    public void NothingInCommon_ScoresLow()
    {
        var score = MatchScorer.Score(
            new TalentProfile("Walvis Bay", "Accountant", ["Taxation"]),
            new RoleListing("Software Developer", "Windhoek", "Technology"));

        score.Should().BeLessThan(20);
    }

    [Fact]
    public void SameCityOnly_ScoresModest()
    {
        var cityOnly = MatchScorer.Score(
            new TalentProfile("Windhoek", "Accountant", []),
            new RoleListing("Software Developer", "Windhoek", "Technology"));

        // City weight is 0.35 → ~35.
        cityOnly.Should().BeInRange(30, 45);
    }

    [Fact]
    public void RoleAffinity_RewardsTitleTokenOverlap()
    {
        var related = MatchScorer.Score(
            new TalentProfile("Rundu", "Network Engineer", []),
            new RoleListing("Network Engineer", "Windhoek", "Telecoms"));
        var unrelated = MatchScorer.Score(
            new TalentProfile("Rundu", "Chef", []),
            new RoleListing("Network Engineer", "Windhoek", "Telecoms"));

        related.Should().BeGreaterThan(unrelated);
    }

    [Fact]
    public void Score_IsAlwaysBetween0And100()
    {
        var score = MatchScorer.Score(
            new TalentProfile(string.Empty, string.Empty, []),
            new RoleListing(string.Empty, string.Empty, string.Empty));

        score.Should().BeInRange(0, 100);
    }
}
