namespace Illumin360.Employers.Domain;

/// <summary>
/// A team member's authority within an employer account. <see cref="Owner"/> can manage the team and
/// billing, <see cref="Recruiter"/> can manage requisitions and candidates, <see cref="Viewer"/> is
/// read-only. Every employer must retain at least one <see cref="Owner"/>.
/// </summary>
public enum EmployerRole
{
    /// <summary>Full control, including team management. At least one must always exist.</summary>
    Owner,

    /// <summary>Can manage requisitions, pipelines and candidates, but not the team.</summary>
    Recruiter,

    /// <summary>Read-only access.</summary>
    Viewer,
}

/// <summary>Parsing helpers for <see cref="EmployerRole"/>.</summary>
public static class EmployerRoles
{
    /// <summary>Parses a role name case-insensitively (owner/recruiter/viewer).</summary>
    /// <param name="value">The role name.</param>
    /// <param name="role">The parsed role when successful.</param>
    /// <returns>True if <paramref name="value"/> is a recognised role.</returns>
    public static bool TryParse(string? value, out EmployerRole role)
        => Enum.TryParse(value, ignoreCase: true, out role) && Enum.IsDefined(role);

    /// <summary>The canonical lower-case wire name for a role (e.g. <c>owner</c>).</summary>
    /// <param name="role">The role.</param>
    /// <returns>The lower-case name.</returns>
    public static string ToWire(this EmployerRole role) => role.ToString().ToLowerInvariant();
}
