using Illumin360.SharedKernel;

namespace Illumin360.Payments.Domain;

/// <summary>Milestone lifecycle. Money transitions (Fund/Approve/Refund) run through the payment provider.</summary>
public enum MilestoneStatus
{
    /// <summary>Created; not yet funded.</summary>
    Pending,

    /// <summary>Client funded it — money held in escrow at the provider.</summary>
    Funded,

    /// <summary>Talent submitted the deliverable; awaiting client approval.</summary>
    Submitted,

    /// <summary>Client approved; funds released to the talent (terminal).</summary>
    Approved,

    /// <summary>Funds returned to the client (terminal).</summary>
    Refunded,
}

/// <summary>
/// One deliverable + payment stage of a fixed-price <see cref="Contract"/>: the client funds it, the talent
/// submits, the client approves (releasing the held funds). Amounts are integer minor units (e.g. cents) —
/// never floats. The actual money movement is performed by the payment provider; this aggregate owns the
/// state machine and guards illegal transitions.
/// </summary>
public sealed class Milestone : Entity<MilestoneId>
{
    private Milestone(MilestoneId id)
        : base(id)
    {
    }

    /// <summary>Owning contract.</summary>
    public ContractId ContractId { get; private set; }

    /// <summary>Order within the contract.</summary>
    public int Order { get; private set; }

    /// <summary>Human title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Amount in minor units (e.g. cents).</summary>
    public long AmountMinor { get; private set; }

    /// <summary>Milestone status.</summary>
    public MilestoneStatus Status { get; private set; }

    /// <summary>Provider reference for the escrow hold (set on funding).</summary>
    public string? HoldReference { get; private set; }

    /// <summary>When created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When funded (UTC), if applicable.</summary>
    public DateTimeOffset? FundedAt { get; private set; }

    /// <summary>When submitted (UTC), if applicable.</summary>
    public DateTimeOffset? SubmittedAt { get; private set; }

    /// <summary>When decided — approved/refunded (UTC), if applicable.</summary>
    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>Adds a milestone to a contract.</summary>
    /// <param name="contractId">Owning contract id.</param>
    /// <param name="order">Order within the contract.</param>
    /// <param name="title">Title (required, ≤ 160 chars).</param>
    /// <param name="amountMinor">Amount in minor units (&gt; 0).</param>
    /// <param name="now">Creation timestamp (UTC).</param>
    /// <returns>The milestone, or a validation error.</returns>
    public static Result<Milestone> Create(ContractId contractId, int order, string title, long amountMinor, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 160)
        {
            return Error.Validation("milestone.title_invalid", "A title (≤ 160 chars) is required.");
        }

        if (amountMinor <= 0)
        {
            return Error.Validation("milestone.amount_invalid", "The amount must be greater than zero.");
        }

        return new Milestone(MilestoneId.New())
        {
            ContractId = contractId,
            Order = order,
            Title = title.Trim(),
            AmountMinor = amountMinor,
            Status = MilestoneStatus.Pending,
            CreatedAt = now,
        };
    }

    /// <summary>Marks the milestone funded once the provider hold succeeds (Pending → Funded).</summary>
    /// <param name="holdReference">The provider's hold reference.</param>
    /// <param name="now">Reference time (UTC).</param>
    /// <returns>Success, or a conflict if not pending.</returns>
    public Result<Milestone> MarkFunded(string holdReference, DateTimeOffset now)
    {
        if (Status != MilestoneStatus.Pending)
        {
            return Error.Conflict("milestone.not_pending", "Only a pending milestone can be funded.");
        }

        Status = MilestoneStatus.Funded;
        HoldReference = holdReference;
        FundedAt = now;
        return this;
    }

    /// <summary>Records the talent's submission (Funded → Submitted).</summary>
    /// <param name="now">Reference time (UTC).</param>
    /// <returns>Success, or a conflict if not funded.</returns>
    public Result<Milestone> Submit(DateTimeOffset now)
    {
        if (Status != MilestoneStatus.Funded)
        {
            return Error.Conflict("milestone.not_funded", "The milestone must be funded before it can be submitted.");
        }

        Status = MilestoneStatus.Submitted;
        SubmittedAt = now;
        return this;
    }

    /// <summary>Approves + releases the milestone once the provider transfer succeeds (Submitted → Approved).</summary>
    /// <param name="now">Reference time (UTC).</param>
    /// <returns>Success, or a conflict if not submitted.</returns>
    public Result<Milestone> MarkApproved(DateTimeOffset now)
    {
        if (Status != MilestoneStatus.Submitted)
        {
            return Error.Conflict("milestone.not_submitted", "Only a submitted milestone can be approved.");
        }

        Status = MilestoneStatus.Approved;
        DecidedAt = now;
        return this;
    }

    /// <summary>Refunds the milestone to the client (from Funded or Submitted).</summary>
    /// <param name="now">Reference time (UTC).</param>
    /// <returns>Success, or a conflict if not refundable.</returns>
    public Result<Milestone> MarkRefunded(DateTimeOffset now)
    {
        if (Status is not (MilestoneStatus.Funded or MilestoneStatus.Submitted))
        {
            return Error.Conflict("milestone.not_refundable", "Only a funded or submitted milestone can be refunded.");
        }

        Status = MilestoneStatus.Refunded;
        DecidedAt = now;
        return this;
    }

    /// <summary>Whether this milestone has reached a terminal decision.</summary>
    public bool IsSettled => Status is MilestoneStatus.Approved or MilestoneStatus.Refunded;
}
