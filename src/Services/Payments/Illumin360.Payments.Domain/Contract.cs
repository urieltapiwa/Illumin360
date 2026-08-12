using Illumin360.SharedKernel;

namespace Illumin360.Payments.Domain;

/// <summary>Contract type.</summary>
public enum ContractType
{
    /// <summary>Fixed-price work split into milestones (the v1 model).</summary>
    FixedPrice,
}

/// <summary>Contract lifecycle.</summary>
public enum ContractStatus
{
    /// <summary>Being drafted; milestones can be added.</summary>
    Draft,

    /// <summary>Both parties agreed; funding/work can proceed.</summary>
    Active,

    /// <summary>All milestones approved (terminal).</summary>
    Completed,

    /// <summary>Cancelled before completion (terminal).</summary>
    Cancelled,
}

/// <summary>
/// A fixed-price agreement between a client (employer) and a talent, split into <see cref="Milestone"/>s.
/// Phase 1 of the marketplace transaction layer (see the transaction-layer design doc): the agreement + its
/// lifecycle. Money movement runs through the <c>IPaymentProvider</c> port (a fake in Phase 1), never in the
/// domain. Owned + migration-managed by the Payments service.
/// </summary>
public sealed class Contract : Entity<ContractId>
{
    private Contract(ContractId id)
        : base(id)
    {
    }

    /// <summary>The hiring client (employer/company) id.</summary>
    public Guid ClientId { get; private set; }

    /// <summary>The talent id.</summary>
    public Guid TalentId { get; private set; }

    /// <summary>Optional link to the requisition/opportunity this contract came from.</summary>
    public Guid? RequestId { get; private set; }

    /// <summary>Human title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Contract type.</summary>
    public ContractType Type { get; private set; }

    /// <summary>ISO-4217 currency code.</summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>Contract status.</summary>
    public ContractStatus Status { get; private set; }

    /// <summary>When the contract was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When the contract was last updated (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Creates a draft fixed-price contract.</summary>
    /// <param name="clientId">The client id.</param>
    /// <param name="talentId">The talent id.</param>
    /// <param name="requestId">Optional requisition/opportunity link.</param>
    /// <param name="title">Contract title (required, ≤ 160 chars).</param>
    /// <param name="currency">ISO-4217 currency (3 letters).</param>
    /// <param name="now">Creation timestamp (UTC).</param>
    /// <returns>The contract, or a validation error.</returns>
    public static Result<Contract> Create(Guid clientId, Guid talentId, Guid? requestId, string title, string currency, DateTimeOffset now)
    {
        if (clientId == Guid.Empty || talentId == Guid.Empty)
        {
            return Error.Validation("contract.parties_required", "A client and a talent are required.");
        }

        if (string.IsNullOrWhiteSpace(title) || title.Length > 160)
        {
            return Error.Validation("contract.title_invalid", "A title (≤ 160 chars) is required.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            return Error.Validation("contract.currency_invalid", "A 3-letter ISO currency code is required.");
        }

        return new Contract(ContractId.New())
        {
            ClientId = clientId,
            TalentId = talentId,
            RequestId = requestId,
            Title = title.Trim(),
            Type = ContractType.FixedPrice,
            Currency = currency.Trim().ToUpperInvariant(),
            Status = ContractStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>Activates the contract (Draft → Active), requiring at least one milestone.</summary>
    /// <param name="milestoneCount">How many milestones the contract has.</param>
    /// <param name="now">Reference time (UTC).</param>
    /// <returns>Success, or a validation/conflict error.</returns>
    public Result<Contract> Activate(int milestoneCount, DateTimeOffset now)
    {
        if (Status != ContractStatus.Draft)
        {
            return Error.Conflict("contract.not_draft", "Only a draft contract can be activated.");
        }

        if (milestoneCount == 0)
        {
            return Error.Validation("contract.no_milestones", "Add at least one milestone before activating.");
        }

        Status = ContractStatus.Active;
        UpdatedAt = now;
        return this;
    }

    /// <summary>Marks the contract complete (all milestones approved).</summary>
    /// <param name="now">Reference time (UTC).</param>
    public void Complete(DateTimeOffset now)
    {
        Status = ContractStatus.Completed;
        UpdatedAt = now;
    }

    /// <summary>Cancels the contract (only before completion).</summary>
    /// <param name="now">Reference time (UTC).</param>
    /// <returns>Success, or a conflict if already terminal.</returns>
    public Result<Contract> Cancel(DateTimeOffset now)
    {
        if (Status is ContractStatus.Completed or ContractStatus.Cancelled)
        {
            return Error.Conflict("contract.terminal", "The contract is already completed or cancelled.");
        }

        Status = ContractStatus.Cancelled;
        UpdatedAt = now;
        return this;
    }

    /// <summary>Whether the contract accepts new milestones (draft only).</summary>
    public bool CanAddMilestones => Status == ContractStatus.Draft;
}
