using FluentAssertions;
using Illumin360.Matching;
using Xunit;

namespace Illumin360.Matching.UnitTests;

public class ReputationScorerTests
{
    [Fact]
    public void No_ratings_is_a_zero_snapshot()
    {
        var s = ReputationScorer.Score([]);
        s.Score.Should().Be(0);
        s.Count.Should().Be(0);
        s.Average.Should().Be(0);
    }

    [Fact]
    public void A_single_five_star_is_shrunk_below_a_perfect_score()
    {
        var s = ReputationScorer.Score([5]);
        s.Count.Should().Be(1);
        s.Average.Should().Be(5);
        // Shrunk toward the 3.5 prior → well under 100.
        s.Score.Should().BeInRange(70, 90);
    }

    [Fact]
    public void More_consistent_five_stars_beat_a_single_five_star()
    {
        var one = ReputationScorer.Score([5]);
        var many = ReputationScorer.Score([5, 5, 5, 5, 5, 5, 5, 5]);
        many.Score.Should().BeGreaterThan(one.Score);
        many.Score.Should().BeGreaterThanOrEqualTo(89);
    }

    [Fact]
    public void A_single_one_star_is_not_fatal()
    {
        var s = ReputationScorer.Score([1]);
        s.Score.Should().BeGreaterThan(20);
    }

    [Fact]
    public void Out_of_range_ratings_are_clamped()
    {
        var s = ReputationScorer.Score([9, 9, 9]);
        s.Average.Should().Be(5);
        s.Score.Should().BeInRange(0, 100);
    }
}
