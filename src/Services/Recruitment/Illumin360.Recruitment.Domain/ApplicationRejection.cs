using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// A free-text reason recorded when an application is rejected. Kept in a service-owned side-table keyed
/// 1:1 by application id, because the <c>applications</c> table is externally seeded (no writable column
/// for a reason).
/// </summary>
public sealed class ApplicationRejection : Entity<Guid>
{
    private ApplicationRejection(Guid id)
        : base(id)
    {
    }

    /// <summary>The rejected application.</summary>
    public Guid ApplicationId { get; private init; }

    /// <summary>The free-text rejection reason.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>Who recorded the rejection, if known.</summary>
    public string? RejectedBy { get; private set; }

    /// <summary>When the rejection was recorded (UTC).</summary>
    public DateTimeOffset RejectedAt { get; private set; }

    /// <summary>Creates a rejection record.</summary>
    /// <param name="applicationId">The application (required).</param>
    /// <param name="reason">The reason (required, ≤ 1000 chars).</param>
    /// <param name="rejectedBy">Who rejected, if known.</param>
    /// <param name="rejectedAt">Timestamp (UTC).</param>
    /// <returns>The rejection, or a validation error.</returns>
    public static Result<ApplicationRejection> Create(Guid applicationId, string reason, string? rejectedBy, DateTimeOffset rejectedAt)
    {
        if (applicationId == Guid.Empty)
        {
            return Error.Validation("rejection.application_required", "An application id is required.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Error.Validation("rejection.reason_required", "A rejection reason is required.");
        }

        if (reason.Length > 1000)
        {
            return Error.Validation("rejection.reason_too_long", "A reason must be 1000 characters or fewer.");
        }

        return new ApplicationRejection(Guid.NewGuid())
        {
            ApplicationId = applicationId,
            Reason = reason.Trim(),
            RejectedBy = string.IsNullOrWhiteSpace(rejectedBy) ? null : rejectedBy.Trim(),
            RejectedAt = rejectedAt,
        };
    }
}
