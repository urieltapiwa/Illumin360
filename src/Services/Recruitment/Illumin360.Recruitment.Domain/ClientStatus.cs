namespace Illumin360.Recruitment.Domain;

/// <summary>Lifecycle stage of a CRM client relationship.</summary>
public enum ClientStatus
{
    /// <summary>A potential client not yet engaged.</summary>
    Prospect,

    /// <summary>An engaged, active client.</summary>
    Active,

    /// <summary>A dormant / former client.</summary>
    Inactive,
}

/// <summary>Parsing helpers for <see cref="ClientStatus"/>.</summary>
public static class ClientStatuses
{
    /// <summary>Parses a status name case-insensitively (prospect/active/inactive).</summary>
    /// <param name="value">The status name.</param>
    /// <param name="status">The parsed status when successful.</param>
    /// <returns>True if <paramref name="value"/> is a recognised status.</returns>
    public static bool TryParse(string? value, out ClientStatus status)
        => Enum.TryParse(value, ignoreCase: true, out status) && Enum.IsDefined(status);

    /// <summary>The canonical lower-case wire name for a status (e.g. <c>prospect</c>).</summary>
    /// <param name="status">The status.</param>
    /// <returns>The lower-case name.</returns>
    public static string ToWire(this ClientStatus status) => status.ToString().ToLowerInvariant();
}
