using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class RequisitionApprovalsTests
{
    private static RecruitmentRequest ARequest()
        => RecruitmentRequest.Post(Guid.NewGuid(), "Software Developer", "Windhoek", 2).Value!;

    [Fact]
    public void Lifecycle_submit_then_approve()
    {
        var a = RequisitionApproval.Create(Guid.NewGuid()).Value!;
        a.Status.Should().Be(ApprovalStatus.Draft);
        a.Submit(DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
        a.Status.Should().Be(ApprovalStatus.Submitted);
        a.Approve("Boss", DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
        a.Status.Should().Be(ApprovalStatus.Approved);
        a.Approver.Should().Be("Boss");
    }

    [Fact]
    public void Cannot_approve_a_draft()
    {
        var a = RequisitionApproval.Create(Guid.NewGuid()).Value!;
        var result = a.Approve("Boss", DateTimeOffset.UnixEpoch);
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void Reject_requires_reason_then_allows_resubmit()
    {
        var a = RequisitionApproval.Create(Guid.NewGuid()).Value!;
        a.Submit(DateTimeOffset.UnixEpoch);
        a.Reject("Boss", "  ", DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        a.Reject("Boss", "Budget not approved", DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
        a.Status.Should().Be(ApprovalStatus.Rejected);
        // A rejected requisition can be resubmitted.
        a.Submit(DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
        a.Status.Should().Be(ApprovalStatus.Submitted);
        a.Reason.Should().BeNull();
    }

    [Fact]
    public async Task Transition_creates_row_on_first_submit()
    {
        var request = ARequest();
        RequisitionApproval? stored = null;
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetByIdAsync(Arg.Any<RequestId>(), Arg.Any<CancellationToken>()).Returns(request);
        repo.GetApprovalAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ => stored);
        repo.When(r => r.AddApproval(Arg.Any<RequisitionApproval>())).Do(ci => stored = ci.Arg<RequisitionApproval>());
        var handler = new TransitionApprovalCommandHandler(repo);

        var result = await handler.HandleAsync(new TransitionApprovalCommand(request.Id.Value, ApprovalAction.Submit, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("submitted");
        repo.Received(1).AddApproval(Arg.Any<RequisitionApproval>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transition_on_missing_request_is_not_found()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetByIdAsync(Arg.Any<RequestId>(), Arg.Any<CancellationToken>()).Returns((RecruitmentRequest?)null);
        var handler = new TransitionApprovalCommandHandler(repo);

        var result = await handler.HandleAsync(new TransitionApprovalCommand(Guid.NewGuid(), ApprovalAction.Submit, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Get_defaults_to_draft_when_none()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetApprovalAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RequisitionApproval?)null);
        var handler = new GetApprovalQueryHandler(repo);

        var result = await handler.HandleAsync(new GetApprovalQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("draft");
    }
}
