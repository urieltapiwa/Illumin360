namespace Illumin360.Recruitment.Domain;

/// <summary>Lifecycle status of a recruitment request.</summary>
public enum RequestStatus
{
    /// <summary>Open and accepting applications.</summary>
    Open,

    /// <summary>Filled — the positions have been hired.</summary>
    Filled,

    /// <summary>Closed without being filled.</summary>
    Closed,
}
