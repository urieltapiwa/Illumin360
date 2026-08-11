using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class RediscoveryTests
{
    private static readonly Guid TargetId = Guid.NewGuid();

    private static RecruitmentRequest Target()
        => RecruitmentRequest.Post(Guid.NewGuid(), "Senior Software Engineer", "Windhoek", 2).Value!;

    private static IRecruitmentRepository RepoWith(
        IReadOnlyList<RediscoveryPoolRow> pool,
        IReadOnlyList<RecruitmentApplication>? current = null)
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetByIdAsync(Arg.Any<RequestId>(), Arg.Any<CancellationToken>()).Returns(Target());
        repo.ListApplicationsAsync(Arg.Any<RequestId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(current ?? []);
        repo.ListRediscoveryPoolAsync(Arg.Any<RequestId>(), Arg.Any<CancellationToken>()).Returns(pool);
        return repo;
    }

    [Fact]
    public async Task Returns_not_found_when_the_target_requisition_is_missing()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetByIdAsync(Arg.Any<RequestId>(), Arg.Any<CancellationToken>()).Returns((RecruitmentRequest?)null);
        var handler = new GetRediscoveryQueryHandler(repo);

        var result = await handler.HandleAsync(new GetRediscoveryQuery(TargetId, 10), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("recruitment.request_not_found");
    }

    [Fact]
    public async Task Ranks_the_strongest_silver_medalist_first()
    {
        var strong = Guid.NewGuid();
        var weak = Guid.NewGuid();
        var pool = new[]
        {
            new RediscoveryPoolRow(weak, "professional", Guid.NewGuid(), "Warehouse Picker", "Swakopmund", "rejected", 25m, 0, false),
            new RediscoveryPoolRow(strong, "professional", Guid.NewGuid(), "Senior Software Engineer", "Windhoek", "rejected", 82m, 3, true),
        };
        var handler = new GetRediscoveryQueryHandler(RepoWith(pool));

        var result = await handler.HandleAsync(new GetRediscoveryQuery(TargetId, 10), CancellationToken.None);

        result.Value!.Should().HaveCount(2);
        result.Value![0].TalentId.Should().Be(strong);
        result.Value![0].Score.Should().BeGreaterThan(result.Value![1].Score);
    }

    [Fact]
    public async Task Excludes_talents_already_in_the_target_pipeline()
    {
        var applicant = Guid.NewGuid();
        var current = new[] { RecruitmentApplication.Apply(new RequestId(TargetId), applicant, "professional", DateTimeOffset.UnixEpoch) };
        var pool = new[]
        {
            new RediscoveryPoolRow(applicant, "professional", Guid.NewGuid(), "Senior Software Engineer", "Windhoek", "rejected", 80m, 2, true),
        };
        var handler = new GetRediscoveryQueryHandler(RepoWith(pool, current));

        var result = await handler.HandleAsync(new GetRediscoveryQuery(TargetId, 10), CancellationToken.None);

        result.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task Keeps_only_a_talents_strongest_prior_application()
    {
        var talent = Guid.NewGuid();
        var pool = new[]
        {
            new RediscoveryPoolRow(talent, "professional", Guid.NewGuid(), "Junior Clerk", "Swakopmund", "rejected", 30m, 0, false),
            new RediscoveryPoolRow(talent, "professional", Guid.NewGuid(), "Senior Software Engineer", "Windhoek", "rejected", 80m, 3, true),
        };
        var handler = new GetRediscoveryQueryHandler(RepoWith(pool));

        var result = await handler.HandleAsync(new GetRediscoveryQuery(TargetId, 10), CancellationToken.None);

        result.Value!.Should().ContainSingle();
        result.Value![0].PriorTitle.Should().Be("Senior Software Engineer");
    }
}
