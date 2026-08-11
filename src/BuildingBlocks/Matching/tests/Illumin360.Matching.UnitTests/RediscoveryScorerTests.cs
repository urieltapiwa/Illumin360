using FluentAssertions;
using Illumin360.Matching;
using Xunit;

namespace Illumin360.Matching.UnitTests;

public class RediscoveryScorerTests
{
    [Fact]
    public void A_near_identical_prior_role_with_an_offer_scores_high()
    {
        var s = RediscoveryScorer.Evaluate(
            targetTitle: "Senior Software Engineer",
            targetCity: "Windhoek",
            priorTitle: "Senior Software Engineer",
            priorCity: "Windhoek",
            priorMatchScore: 80m,
            interviewCount: 3,
            hadOffer: true);

        s.Value.Should().BeGreaterThan(85);
        s.Reason.Should().Contain("offer");
    }

    [Fact]
    public void An_unrelated_cold_prior_role_scores_low()
    {
        var s = RediscoveryScorer.Evaluate(
            targetTitle: "Senior Software Engineer",
            targetCity: "Windhoek",
            priorTitle: "Warehouse Picker",
            priorCity: "Cape Town",
            priorMatchScore: 20m,
            interviewCount: 0,
            hadOffer: false);

        s.Value.Should().BeLessThan(20);
    }

    [Fact]
    public void An_offer_outranks_a_mere_application_all_else_equal()
    {
        var offered = RediscoveryScorer.Evaluate("Data Analyst", "Windhoek", "Data Analyst", "Windhoek", 60m, 2, true);
        var applied = RediscoveryScorer.Evaluate("Data Analyst", "Windhoek", "Data Analyst", "Windhoek", 60m, 0, false);

        offered.Value.Should().BeGreaterThan(applied.Value);
    }

    [Fact]
    public void Same_city_beats_different_city_all_else_equal()
    {
        var same = RediscoveryScorer.Evaluate("Nurse", "Windhoek", "Nurse", "Windhoek", 50m, 1, false);
        var different = RediscoveryScorer.Evaluate("Nurse", "Windhoek", "Nurse", "Swakopmund", 50m, 1, false);

        same.Value.Should().BeGreaterThan(different.Value);
        same.Reason.Should().Contain("same city");
    }

    [Fact]
    public void Score_is_bounded_0_to_100()
    {
        var s = RediscoveryScorer.Evaluate("X", "Y", "X", "Y", 999m, 999, true);
        s.Value.Should().BeInRange(0, 100);
    }
}
