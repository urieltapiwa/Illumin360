using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class TalentApplicationsTests
{
    [Fact]
    public async Task Enriches_applications_with_role_title_and_city()
    {
        var talentId = Guid.NewGuid();
        var request = RecruitmentRequest.Post(Guid.NewGuid(), "Software Developer", "Windhoek", 2).Value!;
        var application = RecruitmentApplication.Apply(request.Id, talentId, "professional", DateTimeOffset.UnixEpoch);

        var repo = Substitute.For<IRecruitmentRepository>();
        repo.ListApplicationsForTalentAsync(talentId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { application });
        repo.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);

        var handler = new GetTalentApplicationsQueryHandler(repo);
        var result = await handler.HandleAsync(new GetTalentApplicationsQuery(talentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var rows = result.Value!;
        rows.Should().ContainSingle();
        rows[0].RoleTitle.Should().Be("Software Developer");
        rows[0].City.Should().Be("Windhoek");
        rows[0].Status.Should().Be("applied");
    }
}
