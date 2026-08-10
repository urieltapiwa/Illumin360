using Illumin360.Employers.Application.Abstractions;
using Illumin360.Employers.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Employers.Application.Employers;

/// <summary>A member of an employer's team.</summary>
/// <param name="Id">Member id.</param>
/// <param name="EmployerId">Owning employer id.</param>
/// <param name="Email">Member email.</param>
/// <param name="DisplayName">Member display name.</param>
/// <param name="Role">Role (owner/recruiter/viewer).</param>
/// <param name="InvitedAt">When the member was invited (UTC).</param>
public sealed record TeamMemberDto(Guid Id, Guid EmployerId, string Email, string DisplayName, string Role, DateTimeOffset InvitedAt)
{
    /// <summary>Projects a domain <see cref="TeamMember"/> into the transport DTO.</summary>
    /// <param name="m">The member.</param>
    /// <returns>The transport DTO.</returns>
    public static TeamMemberDto FromDomain(TeamMember m)
    {
        ArgumentNullException.ThrowIfNull(m);
        return new TeamMemberDto(m.Id.Value, m.EmployerId.Value, m.Email, m.DisplayName, m.Role.ToWire(), m.InvitedAt);
    }
}

/// <summary>Lists the current ("me") employer's team members.</summary>
public sealed record ListTeamMembersQuery : IQuery<IReadOnlyList<TeamMemberDto>>;

/// <summary>Invites a new member to the current ("me") employer's team.</summary>
/// <param name="Email">Member email.</param>
/// <param name="DisplayName">Member display name.</param>
/// <param name="Role">Role (owner/recruiter/viewer).</param>
public sealed record InviteTeamMemberCommand(string Email, string DisplayName, string Role) : ICommand<TeamMemberDto>;

/// <summary>Changes a member's role within the current ("me") employer.</summary>
/// <param name="MemberId">Member id.</param>
/// <param name="Role">New role (owner/recruiter/viewer).</param>
public sealed record ChangeTeamMemberRoleCommand(Guid MemberId, string Role) : ICommand<TeamMemberDto>;

/// <summary>Removes a member from the current ("me") employer's team.</summary>
/// <param name="MemberId">Member id.</param>
public sealed record RemoveTeamMemberCommand(Guid MemberId) : ICommand<bool>;

/// <summary>Request body for changing a team member's role.</summary>
/// <param name="Role">The new role (owner/recruiter/viewer).</param>
public sealed record ChangeRoleRequest(string Role);

/// <summary>Resolves the default ("me") employer id, or a not-found error.</summary>
internal static class MeEmployer
{
    /// <summary>Resolves the default employer's id, or a not-found error if none exists.</summary>
    /// <param name="employers">The employer repository.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The employer id, or a not-found error.</returns>
    public static async Task<Result<EmployerId>> ResolveAsync(IEmployerRepository employers, CancellationToken ct)
    {
        var me = await employers.GetDefaultAsync(ct).ConfigureAwait(false);
        return me is null
            ? Error.NotFound("employer.not_found", "No employer profile found.")
            : Result<EmployerId>.Success(me.Id);
    }
}

/// <summary>Handles <see cref="ListTeamMembersQuery"/>.</summary>
/// <param name="employers">Employer repository (to resolve "me").</param>
/// <param name="team">Team-member repository.</param>
public sealed class ListTeamMembersQueryHandler(IEmployerRepository employers, ITeamMemberRepository team)
    : IQueryHandler<ListTeamMembersQuery, IReadOnlyList<TeamMemberDto>>
{
    private readonly IEmployerRepository _employers = employers;
    private readonly ITeamMemberRepository _team = team;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TeamMemberDto>>> HandleAsync(ListTeamMembersQuery query, CancellationToken cancellationToken)
    {
        var me = await MeEmployer.ResolveAsync(_employers, cancellationToken).ConfigureAwait(false);
        if (me.IsFailure)
        {
            return me.Error!;
        }

        var members = await _team.ListByEmployerAsync(me.Value, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<TeamMemberDto>>.Success(members.Select(TeamMemberDto.FromDomain).ToList());
    }
}

/// <summary>Handles <see cref="InviteTeamMemberCommand"/>.</summary>
/// <param name="employers">Employer repository (to resolve "me").</param>
/// <param name="team">Team-member repository.</param>
public sealed class InviteTeamMemberCommandHandler(IEmployerRepository employers, ITeamMemberRepository team)
    : ICommandHandler<InviteTeamMemberCommand, TeamMemberDto>
{
    private readonly IEmployerRepository _employers = employers;
    private readonly ITeamMemberRepository _team = team;

    /// <inheritdoc />
    public async Task<Result<TeamMemberDto>> HandleAsync(InviteTeamMemberCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var me = await MeEmployer.ResolveAsync(_employers, cancellationToken).ConfigureAwait(false);
        if (me.IsFailure)
        {
            return me.Error!;
        }

        var creation = TeamMember.Invite(me.Value, command.Email, command.DisplayName, command.Role);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        if (await _team.EmailExistsAsync(me.Value, creation.Value!.Email, cancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict("team.email_exists", "A team member with that email already exists.");
        }

        _team.Add(creation.Value!);
        await _team.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return TeamMemberDto.FromDomain(creation.Value!);
    }
}

/// <summary>Handles <see cref="ChangeTeamMemberRoleCommand"/>, preserving the "at least one owner" rule.</summary>
/// <param name="employers">Employer repository (to resolve "me").</param>
/// <param name="team">Team-member repository.</param>
public sealed class ChangeTeamMemberRoleCommandHandler(IEmployerRepository employers, ITeamMemberRepository team)
    : ICommandHandler<ChangeTeamMemberRoleCommand, TeamMemberDto>
{
    private readonly IEmployerRepository _employers = employers;
    private readonly ITeamMemberRepository _team = team;

    /// <inheritdoc />
    public async Task<Result<TeamMemberDto>> HandleAsync(ChangeTeamMemberRoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var me = await MeEmployer.ResolveAsync(_employers, cancellationToken).ConfigureAwait(false);
        if (me.IsFailure)
        {
            return me.Error!;
        }

        var member = await _team.GetTrackedAsync(me.Value, new TeamMemberId(command.MemberId), cancellationToken).ConfigureAwait(false);
        if (member is null)
        {
            return Error.NotFound("team.member_not_found", "No such team member.");
        }

        // Guard the last-owner invariant: demoting the only owner to a non-owner role would orphan the account.
        var demotingOwner = member.Role == EmployerRole.Owner
            && EmployerRoles.TryParse(command.Role, out var target)
            && target != EmployerRole.Owner;
        if (demotingOwner)
        {
            var members = await _team.ListByEmployerAsync(me.Value, cancellationToken).ConfigureAwait(false);
            if (members.Count(m => m.Role == EmployerRole.Owner) <= 1)
            {
                return Error.Conflict("team.last_owner", "An employer must keep at least one owner.");
            }
        }

        var change = member.ChangeRole(command.Role);
        if (change.IsFailure)
        {
            return change.Error!;
        }

        await _team.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return TeamMemberDto.FromDomain(member);
    }
}

/// <summary>Handles <see cref="RemoveTeamMemberCommand"/>, preserving the "at least one owner" rule.</summary>
/// <param name="employers">Employer repository (to resolve "me").</param>
/// <param name="team">Team-member repository.</param>
public sealed class RemoveTeamMemberCommandHandler(IEmployerRepository employers, ITeamMemberRepository team)
    : ICommandHandler<RemoveTeamMemberCommand, bool>
{
    private readonly IEmployerRepository _employers = employers;
    private readonly ITeamMemberRepository _team = team;

    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(RemoveTeamMemberCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var me = await MeEmployer.ResolveAsync(_employers, cancellationToken).ConfigureAwait(false);
        if (me.IsFailure)
        {
            return me.Error!;
        }

        var member = await _team.GetTrackedAsync(me.Value, new TeamMemberId(command.MemberId), cancellationToken).ConfigureAwait(false);
        if (member is null)
        {
            return Error.NotFound("team.member_not_found", "No such team member.");
        }

        if (member.Role == EmployerRole.Owner)
        {
            var members = await _team.ListByEmployerAsync(me.Value, cancellationToken).ConfigureAwait(false);
            if (members.Count(m => m.Role == EmployerRole.Owner) <= 1)
            {
                return Error.Conflict("team.last_owner", "An employer must keep at least one owner.");
            }
        }

        _team.Remove(member);
        await _team.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<bool>.Success(true);
    }
}
