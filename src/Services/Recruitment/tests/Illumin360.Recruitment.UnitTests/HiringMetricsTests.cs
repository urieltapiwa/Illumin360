using FluentAssertions;
using Illumin360.Recruitment.Application.Recruitment;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class HiringMetricsTests
{
    [Fact]
    public void Average_and_median_of_empty_are_zero()
    {
        HiringMath.Average([]).Should().Be(0);
        HiringMath.Median([]).Should().Be(0);
    }

    [Fact]
    public void Average_rounds_to_one_decimal()
        => HiringMath.Average([10, 20, 35]).Should().Be(21.7);

    [Fact]
    public void Median_odd_count_is_middle()
        => HiringMath.Median([30, 10, 20]).Should().Be(20);

    [Fact]
    public void Median_even_count_is_mean_of_middle_two()
        => HiringMath.Median([10, 20, 30, 40]).Should().Be(25);
}
