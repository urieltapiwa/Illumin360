using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>Lifecycle of a proposed interview slot.</summary>
public enum BookingSlotStatus
{
    /// <summary>Offered to the candidate; awaiting selection.</summary>
    Offered,

    /// <summary>The candidate booked this slot (terminal).</summary>
    Booked,

    /// <summary>Superseded when a sibling slot was booked, or withdrawn (terminal).</summary>
    Expired,
}

/// <summary>
/// A recruiter-proposed interview time a candidate can self-book. Several are offered per application; the
/// candidate picks one (which schedules the real <see cref="Interview"/>) and the siblings expire.
/// Owned + migration-managed by the Recruitment service.
/// </summary>
public sealed class InterviewBookingSlot : Entity<Guid>
{
    private InterviewBookingSlot(Guid id)
        : base(id)
    {
    }

    /// <summary>The application this slot is offered for.</summary>
    public Guid ApplicationId { get; private set; }

    /// <summary>Proposed start (UTC).</summary>
    public DateTimeOffset ProposedAt { get; private set; }

    /// <summary>Duration in minutes.</summary>
    public int DurationMinutes { get; private set; }

    /// <summary>Location or mode.</summary>
    public string Location { get; private set; } = string.Empty;

    /// <summary>Slot status.</summary>
    public BookingSlotStatus Status { get; private set; }

    /// <summary>When the slot was offered (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When the slot was booked (UTC), if applicable.</summary>
    public DateTimeOffset? BookedAt { get; private set; }

    /// <summary>Offers a slot for an application.</summary>
    /// <param name="applicationId">The application.</param>
    /// <param name="proposedAt">Proposed start (UTC, must be in the future relative to <paramref name="now"/>).</param>
    /// <param name="durationMinutes">Duration (5–480 minutes).</param>
    /// <param name="location">Location/mode (required).</param>
    /// <param name="now">Reference time (UTC).</param>
    /// <returns>The offered slot, or a validation error.</returns>
    public static Result<InterviewBookingSlot> Offer(Guid applicationId, DateTimeOffset proposedAt, int durationMinutes, string location, DateTimeOffset now)
    {
        if (proposedAt <= now)
        {
            return Error.Validation("slot.past", "A proposed time must be in the future.");
        }

        if (durationMinutes is < 5 or > 480)
        {
            return Error.Validation("slot.duration_invalid", "Duration must be between 5 and 480 minutes.");
        }

        if (string.IsNullOrWhiteSpace(location) || location.Length > 200)
        {
            return Error.Validation("slot.location_invalid", "A location/mode (≤ 200 chars) is required.");
        }

        return new InterviewBookingSlot(Guid.NewGuid())
        {
            ApplicationId = applicationId,
            ProposedAt = proposedAt,
            DurationMinutes = durationMinutes,
            Location = location.Trim(),
            Status = BookingSlotStatus.Offered,
            CreatedAt = now,
        };
    }

    /// <summary>Books this slot (must be offered).</summary>
    /// <param name="now">Reference time (UTC).</param>
    /// <returns>The booked slot, or a conflict if not offered.</returns>
    public Result<InterviewBookingSlot> Book(DateTimeOffset now)
    {
        if (Status != BookingSlotStatus.Offered)
        {
            return Error.Conflict("slot.not_offered", "This slot is no longer available.");
        }

        Status = BookingSlotStatus.Booked;
        BookedAt = now;
        return this;
    }

    /// <summary>Expires this slot if still offered (no-op otherwise).</summary>
    public void Expire()
    {
        if (Status == BookingSlotStatus.Offered)
        {
            Status = BookingSlotStatus.Expired;
        }
    }
}
