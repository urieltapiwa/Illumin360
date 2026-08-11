using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class FeaturedListingsTests
{
    private static RecruitmentRequest ARequest()
        => RecruitmentRequest.Post(Guid.NewGuid(), "Backend Engineer", "Windhoek", 1).Value!;

    [Fact]
    public void IsFeatured_reflects_expiry()
    {
        var now = new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
        var d = RequisitionDetail.Create(Guid.NewGuid(), null, null, null, null, false, now).Value!;

        d.IsFeatured(now).Should().BeFalse(); // no promotion
        d.SetFeaturedUntil(now.AddDays(7));
        d.IsFeatured(now).Should().BeTrue();
        d.IsFeatured(now.AddDays(8)).Should().BeFalse(); // expired
        d.SetFeaturedUntil(null);
        d.IsFeatured(now).Should().BeFalse();
    }

    [Fact]
    public async Task Set_featured_positive_days_sets_expiry_creating_detail_when_absent()
    {
        var requestId = Guid.NewGuid();
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetByIdAsync(Arg.Any<RequestId>(), Arg.Any<CancellationToken>()).Returns(ARequest());
        repo.GetRequisitionDetailAsync(requestId, Arg.Any<CancellationToken>()).Returns((RequisitionDetail?)null);
        repo.ListRequisitionTagsAsync(requestId, Arg.Any<CancellationToken>()).Returns([]);
        var handler = new SetRequisitionFeaturedCommandHandler(repo);

        var result = await handler.HandleAsync(new SetRequisitionFeaturedCommand(requestId, 14), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repo.Received(1).AddRequisitionDetail(Arg.Is<RequisitionDetail>(d => d.FeaturedUntil != null && d.FeaturedUntil > DateTimeOffset.UtcNow));
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_featured_zero_days_clears_existing_promotion()
    {
        var requestId = Guid.NewGuid();
        var existing = RequisitionDetail.Create(requestId, null, null, null, null, false, DateTimeOffset.UtcNow).Value!;
        existing.SetFeaturedUntil(DateTimeOffset.UtcNow.AddDays(5));
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetByIdAsync(Arg.Any<RequestId>(), Arg.Any<CancellationToken>()).Returns(ARequest());
        repo.GetRequisitionDetailAsync(requestId, Arg.Any<CancellationToken>()).Returns(existing);
        repo.ListRequisitionTagsAsync(requestId, Arg.Any<CancellationToken>()).Returns([]);
        var handler = new SetRequisitionFeaturedCommandHandler(repo);

        var result = await handler.HandleAsync(new SetRequisitionFeaturedCommand(requestId, 0), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.FeaturedUntil.Should().BeNull();
    }
}
