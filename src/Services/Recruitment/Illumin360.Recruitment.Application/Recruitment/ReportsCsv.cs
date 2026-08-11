using System.Globalization;
using System.Text;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>
/// Renders recruitment reports as RFC-4180 CSV (pure, unit-testable). Fields containing a comma, quote or
/// newline are quoted and embedded quotes doubled.
/// </summary>
public static class ReportsCsv
{
    /// <summary>Source-of-hire report: one row per talent type with a hire-rate percentage.</summary>
    /// <param name="metrics">The hiring metrics.</param>
    /// <returns>CSV text.</returns>
    public static string SourceOfHire(HiringMetricsDto metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        var sb = new StringBuilder();
        sb.Append("source,applications,hires,hire_rate_pct\r\n");
        foreach (var s in metrics.BySource)
        {
            var rate = s.Applications > 0 ? Math.Round(100.0 * s.Hires / s.Applications, 1) : 0;
            sb.Append(CultureInfo.InvariantCulture, $"{Field(s.Source)},{s.Applications},{s.Hires},{rate.ToString(CultureInfo.InvariantCulture)}\r\n");
        }

        return sb.ToString();
    }

    /// <summary>Pipeline-funnel report: one row per stage with its count.</summary>
    /// <param name="stats">The recruitment stats.</param>
    /// <returns>CSV text.</returns>
    public static string Funnel(RecruitmentStatsDto stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        var sb = new StringBuilder();
        sb.Append("stage,count\r\n");
        foreach (var row in stats.Funnel)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{Field(row.Label)},{row.Count}\r\n");
        }

        return sb.ToString();
    }

    private static string Field(string? value)
    {
        var v = value ?? string.Empty;
        if (v.Contains(',', StringComparison.Ordinal) || v.Contains('"', StringComparison.Ordinal) || v.Contains('\n', StringComparison.Ordinal) || v.Contains('\r', StringComparison.Ordinal))
        {
            return $"\"{v.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return v;
    }
}
