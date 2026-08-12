using FluentAssertions;
using Illumin360.Payments.Application.Abstractions;
using Illumin360.Payments.Application.Payments;
using Illumin360.Payments.Domain;
using Illumin360.Payments.Infrastructure;
using NSubstitute;
using Xunit;

namespace Illumin360.Payments.UnitTests;

public class PayoutAccountTests
{
    [Fact]
    public async Task Register_then_verify_marks_the_account_verified()
    {
        var talent = Guid.NewGuid();
        var repo = Substitute.For<IPaymentsRepository>();
        PayoutAccount? stored = null;
        repo.When(r => r.AddPayoutAccount(Arg.Any<PayoutAccount>())).Do(ci => stored = ci.Arg<PayoutAccount>());
        repo.GetPayoutAccountAsync(talent, Arg.Any<CancellationToken>()).Returns(_ => stored);

        var register = await new RegisterPayoutAccountCommandHandler(repo).HandleAsync(new RegisterPayoutAccountCommand(talent, "sub_1"), CancellationToken.None);
        register.Value!.Status.Should().Be("Pending");

        var verify = await new VerifyPayoutAccountCommandHandler(repo).HandleAsync(new VerifyPayoutAccountCommand(talent), CancellationToken.None);
        verify.Value!.Status.Should().Be("Verified");
    }

    [Fact]
    public async Task Approve_fails_without_a_verified_payout_account()
    {
        var contract = Contract.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Build", "NAD", DateTimeOffset.UnixEpoch).Value!;
        contract.Activate(1, DateTimeOffset.UnixEpoch);
        var milestone = Milestone.Create(contract.Id, 1, "Phase 1", 50000, DateTimeOffset.UnixEpoch).Value!;
        milestone.MarkFunded("hold-1", DateTimeOffset.UnixEpoch);
        milestone.Submit(DateTimeOffset.UnixEpoch);

        var repo = Substitute.For<IPaymentsRepository>();
        repo.GetMilestoneAsync(milestone.Id, Arg.Any<CancellationToken>()).Returns(milestone);
        repo.GetContractAsync(contract.Id, Arg.Any<CancellationToken>()).Returns(contract);
        repo.GetPayoutAccountAsync(contract.TalentId, Arg.Any<CancellationToken>()).Returns((PayoutAccount?)null);

        var result = await new ApproveMilestoneCommandHandler(repo, new FakePaymentProvider(), new MarketplaceOptions()).HandleAsync(new ApproveMilestoneCommand(milestone.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("payments.no_verified_payout_account");
        milestone.Status.Should().Be(MilestoneStatus.Submitted); // unchanged
    }
}
