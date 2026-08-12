using FluentAssertions;
using Illumin360.Payments.Application.Abstractions;
using Illumin360.Payments.Application.Payments;
using Illumin360.Payments.Domain;
using Illumin360.Payments.Infrastructure;
using NSubstitute;
using Xunit;

namespace Illumin360.Payments.UnitTests;

public class ContractLifecycleTests
{
    private static readonly Guid Client = Guid.NewGuid();
    private static readonly Guid Talent = Guid.NewGuid();

    [Fact]
    public void Contract_requires_a_three_letter_currency()
    {
        Contract.Create(Client, Talent, null, "Website build", "dollars", DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        Contract.Create(Client, Talent, null, "Website build", "NAD", DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Contract_cannot_activate_without_milestones()
    {
        var contract = Contract.Create(Client, Talent, null, "Build", "NAD", DateTimeOffset.UnixEpoch).Value!;
        contract.Activate(0, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        contract.Activate(2, DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
        contract.Status.Should().Be(ContractStatus.Active);
    }

    [Fact]
    public void Milestone_state_machine_enforces_order()
    {
        var m = Milestone.Create(ContractId.New(), 1, "Phase 1", 50000, DateTimeOffset.UnixEpoch).Value!;
        // Can't submit before funding.
        m.Submit(DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        m.MarkFunded("hold-1", DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
        // Can't approve before submission.
        m.MarkApproved(DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        m.Submit(DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
        m.MarkApproved(DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
        m.IsSettled.Should().BeTrue();
        // Terminal — no refund after approval.
        m.MarkRefunded(DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
    }

    private static (IPaymentsRepository Repo, Contract Contract, Milestone Milestone) ActiveContractWithMilestone()
    {
        var contract = Contract.Create(Client, Talent, null, "Build", "NAD", DateTimeOffset.UnixEpoch).Value!;
        contract.Activate(1, DateTimeOffset.UnixEpoch);
        var milestone = Milestone.Create(contract.Id, 1, "Phase 1", 50000, DateTimeOffset.UnixEpoch).Value!;
        var repo = Substitute.For<IPaymentsRepository>();
        repo.GetContractAsync(contract.Id, Arg.Any<CancellationToken>()).Returns(contract);
        repo.GetMilestoneAsync(milestone.Id, Arg.Any<CancellationToken>()).Returns(milestone);
        repo.ListMilestonesAsync(contract.Id, Arg.Any<CancellationToken>()).Returns(new[] { milestone });
        return (repo, contract, milestone);
    }

    [Fact]
    public async Task Fund_then_submit_then_approve_releases_and_completes_the_contract()
    {
        var (repo, contract, milestone) = ActiveContractWithMilestone();
        var provider = new FakePaymentProvider();

        (await new FundMilestoneCommandHandler(repo, provider).HandleAsync(new FundMilestoneCommand(milestone.Id.Value), CancellationToken.None)).IsSuccess.Should().BeTrue();
        milestone.Status.Should().Be(MilestoneStatus.Funded);

        (await new SubmitMilestoneCommandHandler(repo).HandleAsync(new SubmitMilestoneCommand(milestone.Id.Value), CancellationToken.None)).IsSuccess.Should().BeTrue();
        milestone.Status.Should().Be(MilestoneStatus.Submitted);

        (await new ApproveMilestoneCommandHandler(repo, provider).HandleAsync(new ApproveMilestoneCommand(milestone.Id.Value), CancellationToken.None)).IsSuccess.Should().BeTrue();
        milestone.Status.Should().Be(MilestoneStatus.Approved);

        // A Fund + a Release movement recorded; contract auto-completed (its only milestone is settled).
        repo.Received(2).AddMovement(Arg.Any<LedgerMovement>());
        contract.Status.Should().Be(ContractStatus.Completed);
    }

    [Fact]
    public async Task Cannot_fund_a_milestone_on_a_draft_contract()
    {
        var contract = Contract.Create(Client, Talent, null, "Build", "NAD", DateTimeOffset.UnixEpoch).Value!; // still Draft
        var milestone = Milestone.Create(contract.Id, 1, "Phase 1", 50000, DateTimeOffset.UnixEpoch).Value!;
        var repo = Substitute.For<IPaymentsRepository>();
        repo.GetMilestoneAsync(milestone.Id, Arg.Any<CancellationToken>()).Returns(milestone);
        repo.GetContractAsync(contract.Id, Arg.Any<CancellationToken>()).Returns(contract);

        var result = await new FundMilestoneCommandHandler(repo, new FakePaymentProvider()).HandleAsync(new FundMilestoneCommand(milestone.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("contract.not_active");
    }

    [Fact]
    public async Task Refund_returns_a_funded_milestone_to_the_client()
    {
        var (repo, _, milestone) = ActiveContractWithMilestone();
        var provider = new FakePaymentProvider();
        await new FundMilestoneCommandHandler(repo, provider).HandleAsync(new FundMilestoneCommand(milestone.Id.Value), CancellationToken.None);

        var result = await new RefundMilestoneCommandHandler(repo, provider).HandleAsync(new RefundMilestoneCommand(milestone.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        milestone.Status.Should().Be(MilestoneStatus.Refunded);
    }
}
