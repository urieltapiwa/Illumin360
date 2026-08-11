using Illumin360.SharedKernel;

namespace Illumin360.Admin.Domain;

/// <summary>
/// An append-only record of an administrative action, for the viewable audit trail. Owned +
/// migration-managed by the Admin service.
/// </summary>
public sealed class AuditEntry : Entity<Guid>
{
    private AuditEntry(Guid id)
        : base(id)
    {
    }

    /// <summary>Who performed the action (admin username / email).</summary>
    public string Actor { get; private init; } = string.Empty;

    /// <summary>Stable action code (e.g. <c>verification.approved</c>).</summary>
    public string Action { get; private init; } = string.Empty;

    /// <summary>The kind of entity acted on (e.g. <c>verification</c>).</summary>
    public string EntityType { get; private init; } = string.Empty;

    /// <summary>The acted-on entity's id, if applicable.</summary>
    public string? EntityId { get; private init; }

    /// <summary>Human-readable summary of what happened.</summary>
    public string Summary { get; private init; } = string.Empty;

    /// <summary>When the action occurred (UTC).</summary>
    public DateTimeOffset OccurredAt { get; private init; }

    /// <summary>Creates an audit entry.</summary>
    /// <param name="actor">Acting admin (defaults to "system" when blank).</param>
    /// <param name="action">Stable action code.</param>
    /// <param name="entityType">Entity kind.</param>
    /// <param name="entityId">Entity id, if any.</param>
    /// <param name="summary">Human-readable summary.</param>
    /// <param name="occurredAt">Timestamp (UTC).</param>
    /// <returns>The audit entry.</returns>
    public static AuditEntry Record(string? actor, string action, string entityType, string? entityId, string summary, DateTimeOffset occurredAt)
        => new(Guid.NewGuid())
        {
            Actor = string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Summary = summary,
            OccurredAt = occurredAt,
        };
}
