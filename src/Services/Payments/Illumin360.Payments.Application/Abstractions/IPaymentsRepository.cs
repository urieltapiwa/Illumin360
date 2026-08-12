using Illumin360.Payments.Domain;

namespace Illumin360.Payments.Application.Abstractions;

/// <summary>Port for Payments persistence (ports &amp; adapters — charter Part 5).</summary>
public interface IPaymentsRepository
{
    /// <summary>Stages a new contract for insertion.</summary>
    /// <param name="contract">The contract.</param>
    void AddContract(Contract contract);

    /// <summary>Loads a contract by id (change-tracked), or null.</summary>
    /// <param name="id">The contract id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Contract?> GetContractAsync(ContractId id, CancellationToken cancellationToken);

    /// <summary>Lists contracts, newest first, optionally filtered by client or talent.</summary>
    /// <param name="clientId">Optional client filter.</param>
    /// <param name="talentId">Optional talent filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Contract>> ListContractsAsync(Guid? clientId, Guid? talentId, CancellationToken cancellationToken);

    /// <summary>Stages a new milestone for insertion.</summary>
    /// <param name="milestone">The milestone.</param>
    void AddMilestone(Milestone milestone);

    /// <summary>Loads a milestone by id (change-tracked), or null.</summary>
    /// <param name="id">The milestone id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Milestone?> GetMilestoneAsync(MilestoneId id, CancellationToken cancellationToken);

    /// <summary>Lists a contract's milestones in order.</summary>
    /// <param name="contractId">The contract id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Milestone>> ListMilestonesAsync(ContractId contractId, CancellationToken cancellationToken);

    /// <summary>Records a ledger movement.</summary>
    /// <param name="movement">The movement.</param>
    void AddMovement(LedgerMovement movement);

    /// <summary>Lists a contract's ledger movements, oldest first.</summary>
    /// <param name="contractId">The contract id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<LedgerMovement>> ListMovementsAsync(ContractId contractId, CancellationToken cancellationToken);

    /// <summary>Commits staged changes.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
