using Illumin360.SharedKernel;

namespace Illumin360.Employers.Domain;

/// <summary>
/// A person with access to an employer account, in one of the <see cref="EmployerRole"/>s. Members belong
/// to a single <see cref="Employer"/> (by <see cref="EmployerId"/>); the "at least one owner" invariant is
/// enforced by the team use-cases, which have the full member set in view.
/// </summary>
public sealed class TeamMember : Entity<TeamMemberId>
{
    private TeamMember(TeamMemberId id)
        : base(id)
    {
    }

    /// <summary>The employer this member belongs to.</summary>
    public EmployerId EmployerId { get; private init; }

    /// <summary>The member's email (unique within an employer, lower-cased).</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>The member's display name.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>The member's role within the employer account.</summary>
    public EmployerRole Role { get; private set; }

    /// <summary>When the member was invited (UTC).</summary>
    public DateTimeOffset InvitedAt { get; private init; }

    /// <summary>Invites a new member to an employer account.</summary>
    /// <param name="employerId">The owning employer.</param>
    /// <param name="email">The member's email (required, must look like an address).</param>
    /// <param name="displayName">The member's display name (required).</param>
    /// <param name="role">The role name (owner/recruiter/viewer).</param>
    /// <returns>The member, or a validation error.</returns>
    public static Result<TeamMember> Invite(EmployerId employerId, string email, string displayName, string role)
    {
        if (string.IsNullOrWhiteSpace(email) || !LooksLikeEmail(email))
        {
            return Error.Validation("team.email_invalid", "A valid email address is required.");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Error.Validation("team.name_required", "A display name is required.");
        }

        if (!EmployerRoles.TryParse(role, out var parsed))
        {
            return Error.Validation("team.role_invalid", "Role must be one of owner, recruiter or viewer.");
        }

        return new TeamMember(TeamMemberId.New())
        {
            EmployerId = employerId,
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            Role = parsed,
            InvitedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Rehydrates a fully-specified member for demo seeding / import.</summary>
    /// <param name="id">Identity.</param>
    /// <param name="employerId">Owning employer.</param>
    /// <param name="email">Email.</param>
    /// <param name="displayName">Display name.</param>
    /// <param name="role">Role.</param>
    /// <param name="invitedAt">Invited timestamp (UTC).</param>
    /// <returns>The hydrated member.</returns>
    public static TeamMember Seed(Guid id, Guid employerId, string email, string displayName, EmployerRole role, DateTimeOffset invitedAt)
        => new(new TeamMemberId(id))
        {
            EmployerId = new EmployerId(employerId),
            Email = email,
            DisplayName = displayName,
            Role = role,
            InvitedAt = invitedAt,
        };

    /// <summary>Changes this member's role.</summary>
    /// <param name="role">The new role name (owner/recruiter/viewer).</param>
    /// <returns>Success, or a validation error.</returns>
    public Result<TeamMember> ChangeRole(string role)
    {
        if (!EmployerRoles.TryParse(role, out var parsed))
        {
            return Error.Validation("team.role_invalid", "Role must be one of owner, recruiter or viewer.");
        }

        Role = parsed;
        return this;
    }

    // Deliberately lightweight: a single '@' with non-empty local and domain parts. Full RFC validation is
    // neither necessary nor desirable here — the invite is confirmed out of band.
    private static bool LooksLikeEmail(string value)
    {
        var at = value.Trim().IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at < value.Trim().Length - 1;
    }
}
