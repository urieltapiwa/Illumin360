using Illumin360.SharedKernel;

namespace Illumin360.Admin.Domain;

/// <summary>Strongly-typed identity for a <see cref="Ticket"/>.</summary>
/// <param name="Value">The underlying GUID.</param>
public readonly record struct TicketId(Guid Value)
{
    /// <summary>Creates a new random identity.</summary>
    /// <returns>A fresh <see cref="TicketId"/>.</returns>
    public static TicketId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Lifecycle state of a support ticket.</summary>
public enum TicketStatus
{
    /// <summary>Unassigned, awaiting triage.</summary>
    Open,

    /// <summary>Assigned to an agent.</summary>
    Assigned,

    /// <summary>Resolved / closed.</summary>
    Resolved,
}

/// <summary>A support ticket an administrator triages (assign → resolve). Aggregate root.</summary>
public sealed class Ticket : Entity<TicketId>
{
    private Ticket(TicketId id)
        : base(id)
    {
    }

    private Ticket(TicketId id, string subject, string priority, string requester)
        : base(id)
    {
        Subject = subject;
        Priority = priority;
        Requester = requester;
        Status = TicketStatus.Open;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Ticket subject.</summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>Priority band (P1/P2/P3).</summary>
    public string Priority { get; private set; } = string.Empty;

    /// <summary>Who raised the ticket.</summary>
    public string Requester { get; private set; } = string.Empty;

    /// <summary>Current lifecycle state.</summary>
    public TicketStatus Status { get; private set; }

    /// <summary>Assigned agent (null while open).</summary>
    public string? Assignee { get; private set; }

    /// <summary>When the ticket was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Rehydrates a ticket from seed/storage with a fixed identity (raises no event).</summary>
    /// <param name="id">Identity.</param>
    /// <param name="subject">Subject.</param>
    /// <param name="priority">Priority band.</param>
    /// <param name="requester">Requester.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The hydrated, open ticket.</returns>
    public static Ticket Seed(Guid id, string subject, string priority, string requester, DateTimeOffset createdAt)
        => new(new TicketId(id))
        {
            Subject = subject,
            Priority = priority,
            Requester = requester,
            Status = TicketStatus.Open,
            CreatedAt = createdAt,
        };

    /// <summary>Assigns the ticket to an agent.</summary>
    /// <param name="assignee">The agent handling the ticket.</param>
    /// <returns>Success, or a conflict error if already resolved.</returns>
    public Result<Ticket> Assign(string assignee)
    {
        if (Status == TicketStatus.Resolved)
        {
            return Error.Conflict("ticket.already_resolved", "Ticket is already resolved.");
        }

        Status = TicketStatus.Assigned;
        Assignee = string.IsNullOrWhiteSpace(assignee) ? "admin" : assignee.Trim();
        Raise(new TicketTriaged(Id, Status.ToString(), Assignee, DateTimeOffset.UtcNow));
        return this;
    }

    /// <summary>Resolves the ticket.</summary>
    /// <param name="resolvedBy">Who resolved it.</param>
    /// <returns>Success, or a conflict error if already resolved.</returns>
    public Result<Ticket> Resolve(string resolvedBy)
    {
        if (Status == TicketStatus.Resolved)
        {
            return Error.Conflict("ticket.already_resolved", "Ticket is already resolved.");
        }

        Status = TicketStatus.Resolved;
        Assignee ??= string.IsNullOrWhiteSpace(resolvedBy) ? "admin" : resolvedBy.Trim();
        Raise(new TicketTriaged(Id, Status.ToString(), Assignee, DateTimeOffset.UtcNow));
        return this;
    }
}

/// <summary>Raised when a ticket is assigned or resolved.</summary>
/// <param name="TicketId">The ticket identity.</param>
/// <param name="Status">New status.</param>
/// <param name="Assignee">Current assignee.</param>
/// <param name="OccurredOn">When it occurred (UTC).</param>
public sealed record TicketTriaged(TicketId TicketId, string Status, string Assignee, DateTimeOffset OccurredOn) : IDomainEvent;
