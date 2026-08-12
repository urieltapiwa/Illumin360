using Illumin360.Payments.Application.Abstractions;
using Illumin360.Payments.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Payments.Application.Payments;

/// <summary>
/// Handles <see cref="FundMilestoneCommand"/> — the client funds escrow. Calls the provider to create a hold,
/// records the money into the domain + ledger only after the provider succeeds. Idempotent on the milestone id.
/// </summary>
/// <param name="repository">The payments repository.</param>
/// <param name="provider">The payment provider.</param>
public sealed class FundMilestoneCommandHandler(IPaymentsRepository repository, IPaymentProvider provider)
    : ICommandHandler<FundMilestoneCommand, MilestoneDto>
{
    private readonly IPaymentsRepository _repository = repository;
    private readonly IPaymentProvider _provider = provider;

    /// <inheritdoc />
    public async Task<Result<MilestoneDto>> HandleAsync(FundMilestoneCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var milestone = await _repository.GetMilestoneAsync(new MilestoneId(command.MilestoneId), cancellationToken).ConfigureAwait(false);
        if (milestone is null)
        {
            return Error.NotFound("milestone.not_found", "Milestone not found.");
        }

        var contract = await _repository.GetContractAsync(milestone.ContractId, cancellationToken).ConfigureAwait(false);
        if (contract is null)
        {
            return Error.NotFound("contract.not_found", "Contract not found.");
        }

        if (contract.Status != ContractStatus.Active)
        {
            return Error.Conflict("contract.not_active", "The contract must be active to fund a milestone.");
        }

        var hold = await _provider.CreateHoldAsync(milestone.Id.ToString(), milestone.AmountMinor, contract.Currency, cancellationToken).ConfigureAwait(false);
        if (!hold.Success)
        {
            return new Error("payment.hold_failed", hold.Error ?? "The payment provider declined the hold.");
        }

        var now = DateTimeOffset.UtcNow;
        var funded = milestone.MarkFunded(hold.Reference, now);
        if (funded.IsFailure)
        {
            return funded.Error!;
        }

        _repository.AddMovement(LedgerMovement.Record(contract.Id, milestone.Id, MovementKind.Fund, milestone.AmountMinor, contract.Currency, hold.Reference, now));
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PaymentsMap.ToDto(milestone);
    }
}

/// <summary>Handles <see cref="SubmitMilestoneCommand"/> — the talent submits a funded milestone (no money).</summary>
/// <param name="repository">The payments repository.</param>
public sealed class SubmitMilestoneCommandHandler(IPaymentsRepository repository)
    : ICommandHandler<SubmitMilestoneCommand, MilestoneDto>
{
    private readonly IPaymentsRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<MilestoneDto>> HandleAsync(SubmitMilestoneCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var milestone = await _repository.GetMilestoneAsync(new MilestoneId(command.MilestoneId), cancellationToken).ConfigureAwait(false);
        if (milestone is null)
        {
            return Error.NotFound("milestone.not_found", "Milestone not found.");
        }

        var submitted = milestone.Submit(DateTimeOffset.UtcNow);
        if (submitted.IsFailure)
        {
            return submitted.Error!;
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PaymentsMap.ToDto(milestone);
    }
}

/// <summary>
/// Handles <see cref="ApproveMilestoneCommand"/> — the client approves + releases escrow to the talent, and
/// completes the contract once every milestone is settled.
/// </summary>
/// <param name="repository">The payments repository.</param>
/// <param name="provider">The payment provider.</param>
public sealed class ApproveMilestoneCommandHandler(IPaymentsRepository repository, IPaymentProvider provider)
    : ICommandHandler<ApproveMilestoneCommand, MilestoneDto>
{
    private readonly IPaymentsRepository _repository = repository;
    private readonly IPaymentProvider _provider = provider;

    /// <inheritdoc />
    public async Task<Result<MilestoneDto>> HandleAsync(ApproveMilestoneCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var milestone = await _repository.GetMilestoneAsync(new MilestoneId(command.MilestoneId), cancellationToken).ConfigureAwait(false);
        if (milestone is null)
        {
            return Error.NotFound("milestone.not_found", "Milestone not found.");
        }

        if (milestone.Status != MilestoneStatus.Submitted)
        {
            return Error.Conflict("milestone.not_submitted", "Only a submitted milestone can be approved.");
        }

        var contract = await _repository.GetContractAsync(milestone.ContractId, cancellationToken).ConfigureAwait(false);
        if (contract is null)
        {
            return Error.NotFound("contract.not_found", "Contract not found.");
        }

        var payout = await _repository.GetPayoutAccountAsync(contract.TalentId, cancellationToken).ConfigureAwait(false);
        if (payout is null || payout.Status != Domain.PayoutAccountStatus.Verified)
        {
            return Error.Validation("payments.no_verified_payout_account", "The talent has no verified payout account.");
        }

        var release = await _provider.ReleaseAsync(
            new ReleaseInstruction(milestone.Id.ToString(), milestone.HoldReference ?? string.Empty, milestone.AmountMinor, contract.Currency, payout.ProviderAccount),
            cancellationToken).ConfigureAwait(false);
        if (!release.Success)
        {
            return new Error("payment.release_failed", release.Error ?? "The payment provider declined the release.");
        }

        var now = DateTimeOffset.UtcNow;
        var approved = milestone.MarkApproved(now);
        if (approved.IsFailure)
        {
            return approved.Error!;
        }

        _repository.AddMovement(LedgerMovement.Record(contract.Id, milestone.Id, MovementKind.Release, milestone.AmountMinor, contract.Currency, release.Reference, now));

        // Complete the contract once every milestone is settled.
        var milestones = await _repository.ListMilestonesAsync(contract.Id, cancellationToken).ConfigureAwait(false);
        if (milestones.All(m => m.Id == milestone.Id || m.IsSettled))
        {
            contract.Complete(now);
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PaymentsMap.ToDto(milestone);
    }
}

/// <summary>Handles <see cref="RefundMilestoneCommand"/> — returns held funds to the client.</summary>
/// <param name="repository">The payments repository.</param>
/// <param name="provider">The payment provider.</param>
public sealed class RefundMilestoneCommandHandler(IPaymentsRepository repository, IPaymentProvider provider)
    : ICommandHandler<RefundMilestoneCommand, MilestoneDto>
{
    private readonly IPaymentsRepository _repository = repository;
    private readonly IPaymentProvider _provider = provider;

    /// <inheritdoc />
    public async Task<Result<MilestoneDto>> HandleAsync(RefundMilestoneCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var milestone = await _repository.GetMilestoneAsync(new MilestoneId(command.MilestoneId), cancellationToken).ConfigureAwait(false);
        if (milestone is null)
        {
            return Error.NotFound("milestone.not_found", "Milestone not found.");
        }

        if (milestone.Status is not (MilestoneStatus.Funded or MilestoneStatus.Submitted))
        {
            return Error.Conflict("milestone.not_refundable", "Only a funded or submitted milestone can be refunded.");
        }

        var contract = await _repository.GetContractAsync(milestone.ContractId, cancellationToken).ConfigureAwait(false);
        if (contract is null)
        {
            return Error.NotFound("contract.not_found", "Contract not found.");
        }

        var refund = await _provider.RefundAsync(
            new RefundInstruction(milestone.Id.ToString(), milestone.HoldReference ?? string.Empty, milestone.AmountMinor, contract.Currency),
            cancellationToken).ConfigureAwait(false);
        if (!refund.Success)
        {
            return new Error("payment.refund_failed", refund.Error ?? "The payment provider declined the refund.");
        }

        var now = DateTimeOffset.UtcNow;
        var refunded = milestone.MarkRefunded(now);
        if (refunded.IsFailure)
        {
            return refunded.Error!;
        }

        _repository.AddMovement(LedgerMovement.Record(contract.Id, milestone.Id, MovementKind.Refund, milestone.AmountMinor, contract.Currency, refund.Reference, now));
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PaymentsMap.ToDto(milestone);
    }
}
