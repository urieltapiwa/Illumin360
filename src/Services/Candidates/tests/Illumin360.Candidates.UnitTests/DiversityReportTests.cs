using FluentAssertions;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using NSubstitute;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class DiversityReportTests
{
    [Fact]
    public async Task Forwards_repository_report()
    {
        var repo = Substitute.For<ICandidateRepository>();
        repo.GetDiversityReportAsync(Arg.Any<CancellationToken>()).Returns(new DiversityReportDto(
            5,
            new[] { new CountByLabel("Namibian", 4), new CountByLabel("Zimbabwean", 1) },
            new[] { new CountByLabel("Windhoek", 3) },
            new[] { new CountByLabel("ActivelyLooking", 5) }));
        var handler = new GetDiversityReportQueryHandler(repo);

        var result = await handler.HandleAsync(new GetDiversityReportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(5);
        result.Value!.ByNationality.Should().ContainSingle(x => x.Label == "Namibian" && x.Count == 4);
        result.Value!.ByCity.Should().ContainSingle(x => x.Label == "Windhoek");
    }
}
