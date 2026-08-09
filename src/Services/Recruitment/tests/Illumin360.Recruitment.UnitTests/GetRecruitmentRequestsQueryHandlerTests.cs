using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class GetRecruitmentRequestsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_MapsDomainToDtos_AndClampsPageSize()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        var request = RecruitmentRequest.Post(Guid.NewGuid(), "Network Engineer", "Windhoek", 2).Value!;
        repo.ListAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([request]);

        var handler = new GetRecruitmentRequestsQueryHandler(repo);
        var result = await handler.HandleAsync(
            new GetRecruitmentRequestsQuery(City: "Windhoek", Status: null, Page: 1, PageSize: 999), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle().Which.City.Should().Be("Windhoek");

        // PageSize 999 must be clamped to 100.
        await repo.Received(1).ListAsync("Windhoek", null, 0, 100, Arg.Any<CancellationToken>());
    }
}
