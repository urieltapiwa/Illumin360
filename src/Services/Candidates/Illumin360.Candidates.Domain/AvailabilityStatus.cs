namespace Illumin360.Candidates.Domain;

/// <summary>
/// A candidate's availability for opportunities. Mirrors the
/// <c>job_seekers.availability_status</c> column in the canonical schema.
/// </summary>
public enum AvailabilityStatus
{
    /// <summary>Actively seeking a new role.</summary>
    ActivelyLooking,
    /// <summary>Employed but open to the right opportunity.</summary>
    OpenToOpportunities,
    /// <summary>Not currently available.</summary>
    NotAvailable,
}
