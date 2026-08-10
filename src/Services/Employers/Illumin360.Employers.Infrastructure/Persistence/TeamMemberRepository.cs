using Illumin360.Employers.Application.Abstractions;
using Illumin360.Employers.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Employers.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="ITeamMemberRepository"/>.</summary>
/// <param name="db">The Employers database context.</param>
public sealed class TeamMemberRepository(EmployersDbContext db) : ITeamMemberRepository
{
    private readonly EmployersDbContext _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<TeamMember>> ListByEmployerAsync(EmployerId employerId, CancellationToken cancellationToken)
        => await _db.TeamMembers.AsNoTracking()
            .Where(m => m.EmployerId == employerId)
            .OrderBy(m => m.InvitedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<TeamMember?> GetTrackedAsync(EmployerId employerId, TeamMemberId id, CancellationToken cancellationToken)
        => await _db.TeamMembers
            .FirstOrDefaultAsync(m => m.EmployerId == employerId && m.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(EmployerId employerId, string email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);
        var normalized = email.Trim().ToLowerInvariant();
        return await _db.TeamMembers.AsNoTracking()
            .AnyAsync(m => m.EmployerId == employerId && m.Email == normalized, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Add(TeamMember member) => _db.TeamMembers.Add(member);

    /// <inheritdoc />
    public void Remove(TeamMember member) => _db.TeamMembers.Remove(member);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
