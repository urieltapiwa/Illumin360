using FluentAssertions;
using Illumin360.Recruitment.Application.Recruitment;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class ReportsCsvTests
{
    [Fact]
    public void SourceOfHire_has_header_and_computes_rate()
    {
        var metrics = new HiringMetricsDto(3, 12.5, 10, new[]
        {
            new SourceMetric("professional", 8, 2),
            new SourceMetric("student", 0, 0),
        });

        var csv = ReportsCsv.SourceOfHire(metrics);

        csv.Should().StartWith("source,applications,hires,hire_rate_pct\r\n");
        csv.Should().Contain("professional,8,2,25\r\n");
        csv.Should().Contain("student,0,0,0\r\n"); // no divide-by-zero
    }

    [Fact]
    public void Funnel_renders_rows()
    {
        var funnel = new[] { new CountByLabel("applied", 10), new CountByLabel("hired", 2) };
        var stats = new RecruitmentStatsDto(0, 0, 0, 0, 0, 0, funnel, [], [], []);

        var csv = ReportsCsv.Funnel(stats);

        csv.Should().StartWith("stage,count\r\n");
        csv.Should().Contain("applied,10\r\n");
        csv.Should().Contain("hired,2\r\n");
    }

    [Fact]
    public void Fields_with_commas_are_quoted()
    {
        var metrics = new HiringMetricsDto(0, 0, 0, [new SourceMetric("agency, external", 1, 1)]);

        var csv = ReportsCsv.SourceOfHire(metrics);

        csv.Should().Contain("\"agency, external\",1,1,100\r\n");
    }
}
