using Illumin360.Payments.Application.Abstractions;
using Illumin360.Payments.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Payments.Application.Payments;

/// <summary>A contract summary.</summary>
public sealed record ContractDto(Guid Id, Guid ClientId, Guid TalentId, Guid? RequestId, string Title, string Currency, string Status, DateTimeOffset CreatedAt);

/// <summary>A milestone.</summary>
public sealed record MilestoneDto(Guid Id, int Order, string Title, long AmountMinor, string Status, DateTimeOffset? FundedAt, DateTimeOffset? SubmittedAt, DateTimeOffset? DecidedAt);

/// <summary>A ledger movement.</summary>
public sealed record MovementDto(Guid Id, Guid MilestoneId, string Kind, long AmountMinor, string Currency, DateTimeOffset CreatedAt);

/// <summary>A contract with its milestones and ledger movements.</summary>
public sealed record ContractDetailDto(ContractDto Contract, IReadOnlyList<MilestoneDto> Milestones, IReadOnlyList<MovementDto> Movements);

/// <summary>Creates a draft fixed-price contract.</summary>
public sealed record CreateContractCommand(Guid ClientId, Guid TalentId, Guid? RequestId, string Title, string Currency) : ICommand<ContractDto>;

/// <summary>Adds a milestone to a draft contract.</summary>
public sealed record AddMilestoneCommand(Guid ContractId, string Title, long AmountMinor) : ICommand<MilestoneDto>;

/// <summary>Activates a draft contract (requires ≥ 1 milestone).</summary>
public sealed record ActivateContractCommand(Guid ContractId) : ICommand<ContractDto>;

/// <summary>Cancels a contract before completion.</summary>
public sealed record CancelContractCommand(Guid ContractId) : ICommand<ContractDto>;

/// <summary>Funds a milestone (client → escrow) via the payment provider.</summary>
public sealed record FundMilestoneCommand(Guid MilestoneId) : ICommand<MilestoneDto>;

/// <summary>Records the talent's submission for a funded milestone.</summary>
public sealed record SubmitMilestoneCommand(Guid MilestoneId) : ICommand<MilestoneDto>;

/// <summary>Approves a submitted milestone, releasing escrow to the talent.</summary>
public sealed record ApproveMilestoneCommand(Guid MilestoneId) : ICommand<MilestoneDto>;

/// <summary>Refunds a funded/submitted milestone to the client.</summary>
public sealed record RefundMilestoneCommand(Guid MilestoneId) : ICommand<MilestoneDto>;

/// <summary>Lists contracts, optionally by client or talent.</summary>
public sealed record ListContractsQuery(Guid? ClientId, Guid? TalentId) : IQuery<IReadOnlyList<ContractDto>>;

/// <summary>Gets a contract with its milestones + movements.</summary>
public sealed record GetContractQuery(Guid ContractId) : IQuery<ContractDetailDto>;

/// <summary>Shared mapping helpers.</summary>
internal static class PaymentsMap
{
    /// <summary>Projects a contract to its DTO.</summary>
    /// <param name="c">The contract.</param>
    /// <returns>The DTO.</returns>
    public static ContractDto ToDto(Contract c)
        => new(c.Id.Value, c.ClientId, c.TalentId, c.RequestId, c.Title, c.Currency, c.Status.ToString(), c.CreatedAt);

    /// <summary>Projects a milestone to its DTO.</summary>
    /// <param name="m">The milestone.</param>
    /// <returns>The DTO.</returns>
    public static MilestoneDto ToDto(Milestone m)
        => new(m.Id.Value, m.Order, m.Title, m.AmountMinor, m.Status.ToString(), m.FundedAt, m.SubmittedAt, m.DecidedAt);

    /// <summary>Projects a ledger movement to its DTO.</summary>
    /// <param name="l">The movement.</param>
    /// <returns>The DTO.</returns>
    public static MovementDto ToDto(LedgerMovement l)
        => new(l.Id, l.MilestoneId.Value, l.Kind.ToString(), l.AmountMinor, l.Currency, l.CreatedAt);
}

/// <summary>Handles <see cref="CreateContractCommand"/>.</summary>
/// <param name="repository">The payments repository.</param>
public sealed class CreateContractCommandHandler(IPaymentsRepository repository) : ICommandHandler<CreateContractCommand, ContractDto>
{
    private readonly IPaymentsRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ContractDto>> HandleAsync(CreateContractCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var contract = Contract.Create(command.ClientId, command.TalentId, command.RequestId, command.Title, command.Currency, DateTimeOffset.UtcNow);
        if (contract.IsFailure)
        {
            return contract.Error!;
        }

        _repository.AddContract(contract.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PaymentsMap.ToDto(contract.Value!);
    }
}

/// <summary>Handles <see cref="AddMilestoneCommand"/> — appends at the next order (draft only).</summary>
/// <param name="repository">The payments repository.</param>
public sealed class AddMilestoneCommandHandler(IPaymentsRepository repository) : ICommandHandler<AddMilestoneCommand, MilestoneDto>
{
    private readonly IPaymentsRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<MilestoneDto>> HandleAsync(AddMilestoneCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var contract = await _repository.GetContractAsync(new ContractId(command.ContractId), cancellationToken).ConfigureAwait(false);
        if (contract is null)
        {
            return Error.NotFound("contract.not_found", "Contract not found.");
        }

        if (!contract.CanAddMilestones)
        {
            return Error.Conflict("contract.not_draft", "Milestones can only be added while the contract is a draft.");
        }

        var existing = await _repository.ListMilestonesAsync(contract.Id, cancellationToken).ConfigureAwait(false);
        var nextOrder = existing.Count == 0 ? 1 : existing.Max(m => m.Order) + 1;

        var milestone = Milestone.Create(contract.Id, nextOrder, command.Title, command.AmountMinor, DateTimeOffset.UtcNow);
        if (milestone.IsFailure)
        {
            return milestone.Error!;
        }

        _repository.AddMilestone(milestone.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PaymentsMap.ToDto(milestone.Value!);
    }
}

/// <summary>Handles <see cref="ActivateContractCommand"/>.</summary>
/// <param name="repository">The payments repository.</param>
public sealed class ActivateContractCommandHandler(IPaymentsRepository repository) : ICommandHandler<ActivateContractCommand, ContractDto>
{
    private readonly IPaymentsRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ContractDto>> HandleAsync(ActivateContractCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var contract = await _repository.GetContractAsync(new ContractId(command.ContractId), cancellationToken).ConfigureAwait(false);
        if (contract is null)
        {
            return Error.NotFound("contract.not_found", "Contract not found.");
        }

        var milestones = await _repository.ListMilestonesAsync(contract.Id, cancellationToken).ConfigureAwait(false);
        var activated = contract.Activate(milestones.Count, DateTimeOffset.UtcNow);
        if (activated.IsFailure)
        {
            return activated.Error!;
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PaymentsMap.ToDto(contract);
    }
}

/// <summary>Handles <see cref="CancelContractCommand"/>.</summary>
/// <param name="repository">The payments repository.</param>
public sealed class CancelContractCommandHandler(IPaymentsRepository repository) : ICommandHandler<CancelContractCommand, ContractDto>
{
    private readonly IPaymentsRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ContractDto>> HandleAsync(CancelContractCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var contract = await _repository.GetContractAsync(new ContractId(command.ContractId), cancellationToken).ConfigureAwait(false);
        if (contract is null)
        {
            return Error.NotFound("contract.not_found", "Contract not found.");
        }

        var cancelled = contract.Cancel(DateTimeOffset.UtcNow);
        if (cancelled.IsFailure)
        {
            return cancelled.Error!;
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PaymentsMap.ToDto(contract);
    }
}

/// <summary>Handles <see cref="ListContractsQuery"/>.</summary>
/// <param name="repository">The payments repository.</param>
public sealed class ListContractsQueryHandler(IPaymentsRepository repository) : IQueryHandler<ListContractsQuery, IReadOnlyList<ContractDto>>
{
    private readonly IPaymentsRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ContractDto>>> HandleAsync(ListContractsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var contracts = await _repository.ListContractsAsync(query.ClientId, query.TalentId, cancellationToken).ConfigureAwait(false);
        return contracts.Select(PaymentsMap.ToDto).ToList();
    }
}

/// <summary>Handles <see cref="GetContractQuery"/>.</summary>
/// <param name="repository">The payments repository.</param>
public sealed class GetContractQueryHandler(IPaymentsRepository repository) : IQueryHandler<GetContractQuery, ContractDetailDto>
{
    private readonly IPaymentsRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ContractDetailDto>> HandleAsync(GetContractQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var contract = await _repository.GetContractAsync(new ContractId(query.ContractId), cancellationToken).ConfigureAwait(false);
        if (contract is null)
        {
            return Error.NotFound("contract.not_found", "Contract not found.");
        }

        var milestones = await _repository.ListMilestonesAsync(contract.Id, cancellationToken).ConfigureAwait(false);
        var movements = await _repository.ListMovementsAsync(contract.Id, cancellationToken).ConfigureAwait(false);
        return new ContractDetailDto(
            PaymentsMap.ToDto(contract),
            milestones.OrderBy(m => m.Order).Select(PaymentsMap.ToDto).ToList(),
            movements.Select(PaymentsMap.ToDto).ToList());
    }
}
