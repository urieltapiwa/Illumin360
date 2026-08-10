using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class RequisitionDetailsTests
{
    private static RecruitmentRequest ARequest()
        => RecruitmentRequest.Post(Guid.NewGuid(), "Software Developer", "Windhoek", 2).Value!;

    [Fact]
    public void Detail_validates_salary_range_and_type()
    {
        var id = Guid.NewGuid();
        RequisitionDetail.Create(id, 90000, 50000, "NAD", "fulltime", true, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        RequisitionDetail.Create(id, -1, 50000, "NAD", "fulltime", true, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        RequisitionDetail.Create(id, 50000, 90000, "nad", "banana", true, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();

        var ok = RequisitionDetail.Create(id, 50000, 90000, "nad", "contract", true, DateTimeOffset.UnixEpoch);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.Currency.Should().Be("NAD");
        ok.Value!.EmploymentType.Should().Be(EmploymentType.Contract);
    }

    [Fact]
    public async Task Set_detail_creates_then_updates()
    {
        var request = ARequest();
        RequisitionDetail? stored = null;
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetByIdAsync(Arg.Any<RequestId>(), Arg.Any<CancellationToken>()).Returns(request);
        repo.GetRequisitionDetailAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ => stored);
        repo.When(r => r.AddRequisitionDetail(Arg.Any<RequisitionDetail>())).Do(ci => stored = ci.Arg<RequisitionDetail>());
        repo.ListRequisitionTagsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        var handler = new SetRequisitionDetailCommandHandler(repo);

        var create = await handler.HandleAsync(new SetRequisitionDetailCommand(request.Id.Value, 40000, 60000, "NAD", "fulltime", true), CancellationToken.None);
        create.IsSuccess.Should().BeTrue();
        create.Value!.Remote.Should().BeTrue();
        repo.Received(1).AddRequisitionDetail(Arg.Any<RequisitionDetail>());

        // Second call updates the existing row (no second insert).
        var update = await handler.HandleAsync(new SetRequisitionDetailCommand(request.Id.Value, 45000, 65000, "NAD", "contract", false), CancellationToken.None);
        update.IsSuccess.Should().BeTrue();
        update.Value!.EmploymentType.Should().Be("contract");
        repo.Received(1).AddRequisitionDetail(Arg.Any<RequisitionDetail>());
    }

    [Fact]
    public async Task Set_detail_on_missing_request_is_not_found()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetByIdAsync(Arg.Any<RequestId>(), Arg.Any<CancellationToken>()).Returns((RecruitmentRequest?)null);
        var handler = new SetRequisitionDetailCommandHandler(repo);

        var result = await handler.HandleAsync(new SetRequisitionDetailCommand(Guid.NewGuid(), 1, 2, "NAD", "fulltime", false), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Add_tag_idempotent_returns_labels()
    {
        var request = ARequest();
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetByIdAsync(Arg.Any<RequestId>(), Arg.Any<CancellationToken>()).Returns(request);
        repo.RequisitionTagExistsAsync(Arg.Any<Guid>(), "urgent", Arg.Any<CancellationToken>()).Returns(true);
        repo.ListRequisitionTagsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new[] { RequisitionTag.Create(request.Id.Value, "urgent").Value! });
        var handler = new AddRequisitionTagCommandHandler(repo);

        var result = await handler.HandleAsync(new AddRequisitionTagCommand(request.Id.Value, "Urgent"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle().Which.Should().Be("urgent");
        repo.DidNotReceive().AddRequisitionTag(Arg.Any<RequisitionTag>());
    }

    [Fact]
    public async Task Get_detail_defaults_when_none_set()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetRequisitionDetailAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RequisitionDetail?)null);
        repo.ListRequisitionTagsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        var handler = new GetRequisitionDetailQueryHandler(repo);

        var result = await handler.HandleAsync(new GetRequisitionDetailQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmploymentType.Should().Be("fulltime");
        result.Value!.SalaryMin.Should().BeNull();
        result.Value!.Tags.Should().BeEmpty();
    }
}
