namespace Illumin360.Recruitment.Domain;

/// <summary>Lifecycle of an <see cref="Offer"/>.</summary>
public enum OfferStatus
{
    /// <summary>Drafted but not yet extended to the candidate.</summary>
    Draft,

    /// <summary>Extended to the candidate, awaiting their decision.</summary>
    Sent,

    /// <summary>Accepted by the candidate (terminal).</summary>
    Accepted,

    /// <summary>Declined by the candidate (terminal).</summary>
    Declined,

    /// <summary>Withdrawn by the employer before a decision (terminal).</summary>
    Withdrawn,
}
