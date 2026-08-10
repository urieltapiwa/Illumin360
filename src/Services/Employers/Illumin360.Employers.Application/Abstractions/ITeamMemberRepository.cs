using Illumin360.Employers.Domain;

namespace Illumin360.Employers.Application.Abstractions;

/// <summary>Persistence port for employer team members.</summary>
public interface ITeamMemberRepository
{
    /// <summary>Lists an employer's members, oldest invite first.</summary>
    /// <param name="employerId">The owning employer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<TeamMember>> ListByEmployerAsync(EmployerId employerId, CancellationToken cancellationToken);

    /// <summary>Loads a member by id for the given employer (change-tracked), or null.</summary>
    /// <param name="employerId">The owning employer.</param>
    /// <param name="id">The member id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TeamMember?> GetTrackedAsync(EmployerId employerId, TeamMemberId id, CancellationToken cancellationToken);

    /// <summary>Whether a member with the given email already exists for the employer.</summary>
    /// <param name="employerId">The owning employer.</param>
    /// <param name="email">The email (compared case-insensitively).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> EmailExistsAsync(EmployerId employerId, string email, CancellationToken cancellationToken);

    /// <summary>Stages a new member for insertion.</summary>
    /// <param name="member">The member to add.</param>
    void Add(TeamMember member);

    /// <summary>Stages a member for removal.</summary>
    /// <param name="member">The member to remove.</param>
    void Remove(TeamMember member);

    /// <summary>Commits staged changes.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
