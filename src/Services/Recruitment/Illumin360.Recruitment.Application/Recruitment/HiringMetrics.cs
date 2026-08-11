using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>Hiring throughput by source (talent type).</summary>
/// <param name="Source">Talent type (e.g. professional/student).</param>
/// <param name="Applications">Applications from this source.</param>
/// <param name="Hires">Hires from this source.</param>
public sealed record SourceMetric(string Source, int Applications, int Hires);

/// <summary>Aggregate hiring metrics: time-to-hire and source-of-hire.</summary>
/// <param name="Hires">Total hires.</param>
/// <param name="AvgTimeToHireDays">Mean days from apply to hire decision.</param>
/// <param name="MedianTimeToHireDays">Median days from apply to hire decision.</param>
/// <param name="BySource">Applications + hires broken down by source.</param>
public sealed record HiringMetricsDto(int Hires, double AvgTimeToHireDays, double MedianTimeToHireDays, IReadOnlyList<SourceMetric> BySource);

/// <summary>Pure statistics helpers for hiring metrics (unit-testable, no EF).</summary>
public static class HiringMath
{
    /// <summary>The arithmetic mean of the values, rounded to one decimal (0 when empty).</summary>
    /// <param name="values">The values.</param>
    /// <returns>The mean.</returns>
    public static double Average(IReadOnlyList<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.Count == 0 ? 0 : Math.Round(values.Average(), 1);
    }

    /// <summary>The median of the values, rounded to one decimal (0 when empty).</summary>
    /// <param name="values">The values.</param>
    /// <returns>The median.</returns>
    public static double Median(IReadOnlyList<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        var median = sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
        return Math.Round(median, 1);
    }
}

/// <summary>Query: aggregate time-to-hire and source-of-hire metrics.</summary>
public sealed record GetHiringMetricsQuery : IQuery<HiringMetricsDto>;

/// <summary>Handles <see cref="GetHiringMetricsQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetHiringMetricsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetHiringMetricsQuery, HiringMetricsDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<HiringMetricsDto>> HandleAsync(GetHiringMetricsQuery query, CancellationToken cancellationToken)
    {
        var metrics = await _repository.GetHiringMetricsAsync(cancellationToken).ConfigureAwait(false);
        return Result<HiringMetricsDto>.Success(metrics);
    }
}
