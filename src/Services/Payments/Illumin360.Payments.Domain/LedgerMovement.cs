using Illumin360.SharedKernel;

namespace Illumin360.Payments.Domain;

/// <summary>The kind of money movement recorded on the ledger.</summary>
public enum MovementKind
{
    /// <summary>Client funded escrow.</summary>
    Fund,

    /// <summary>Funds released to the talent.</summary>
    Release,

    /// <summary>Funds refunded to the client.</summary>
    Refund,

    /// <summary>Platform commission taken from a release (the take-rate).</summary>
    Fee,
}

/// <summary>
/// An append-only ledger movement — the audit trail of money flowing through a milestone. Phase 1 records a
/// single row per movement; Phase 2+ upgrades this to strict double-entry (debit/credit account rows) once
/// real funds flow (see the transaction-layer design doc §3). Never mutated after insertion.
/// </summary>
public sealed class LedgerMovement : Entity<Guid>
{
    private LedgerMovement(Guid id)
        : base(id)
    {
    }

    /// <summary>The contract this movement belongs to.</summary>
    public ContractId ContractId { get; private set; }

    /// <summary>The milestone this movement belongs to.</summary>
    public MilestoneId MilestoneId { get; private set; }

    /// <summary>The movement kind.</summary>
    public MovementKind Kind { get; private set; }

    /// <summary>Amount in minor units.</summary>
    public long AmountMinor { get; private set; }

    /// <summary>ISO-4217 currency.</summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>The provider reference for this movement.</summary>
    public string ProviderReference { get; private set; } = string.Empty;

    /// <summary>When recorded (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Records a movement.</summary>
    /// <param name="contractId">Contract id.</param>
    /// <param name="milestoneId">Milestone id.</param>
    /// <param name="kind">Movement kind.</param>
    /// <param name="amountMinor">Amount in minor units.</param>
    /// <param name="currency">ISO-4217 currency.</param>
    /// <param name="providerReference">Provider reference.</param>
    /// <param name="now">Timestamp (UTC).</param>
    /// <returns>The movement.</returns>
    public static LedgerMovement Record(ContractId contractId, MilestoneId milestoneId, MovementKind kind, long amountMinor, string currency, string providerReference, DateTimeOffset now)
        => new(Guid.NewGuid())
        {
            ContractId = contractId,
            MilestoneId = milestoneId,
            Kind = kind,
            AmountMinor = amountMinor,
            Currency = currency,
            ProviderReference = providerReference,
            CreatedAt = now,
        };
}
