using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// A member of an interview panel (interviewer / hiring manager / observer). Owned + migration-managed by
/// the service; enables multi-attendee "panel" interviews.
/// </summary>
public sealed class InterviewAttendee : Entity<Guid>
{
    private InterviewAttendee(Guid id)
        : base(id)
    {
    }

    /// <summary>The interview this attendee belongs to.</summary>
    public Guid InterviewId { get; private init; }

    /// <summary>Attendee name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Attendee email, if any (lower-cased).</summary>
    public string? Email { get; private set; }

    /// <summary>Panel role (e.g. interviewer, hiring manager, observer).</summary>
    public string Role { get; private set; } = "interviewer";

    /// <summary>When the attendee was added (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Adds a panel attendee to an interview.</summary>
    /// <param name="interviewId">The interview (required).</param>
    /// <param name="name">Attendee name (required).</param>
    /// <param name="email">Optional email (must look like an address when present).</param>
    /// <param name="role">Panel role (defaults to interviewer).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The attendee, or a validation error.</returns>
    public static Result<InterviewAttendee> Create(Guid interviewId, string name, string? email, string? role, DateTimeOffset createdAt)
    {
        if (interviewId == Guid.Empty)
        {
            return Error.Validation("attendee.interview_required", "An interview id is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("attendee.name_required", "An attendee name is required.");
        }

        if (!string.IsNullOrWhiteSpace(email) && !LooksLikeEmail(email))
        {
            return Error.Validation("attendee.email_invalid", "The email address is not valid.");
        }

        return new InterviewAttendee(Guid.NewGuid())
        {
            InterviewId = interviewId,
            Name = name.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(),
            Role = string.IsNullOrWhiteSpace(role) ? "interviewer" : role.Trim().ToLowerInvariant(),
            CreatedAt = createdAt,
        };
    }

    private static bool LooksLikeEmail(string value)
    {
        var trimmed = value.Trim();
        var at = trimmed.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at < trimmed.Length - 1;
    }
}
