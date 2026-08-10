using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Candidates.Infrastructure.Persistence;

/// <summary>EF Core adapter implementing <see cref="ICandidateRepository"/>.</summary>
public sealed class CandidateRepository(CandidatesDbContext db) : ICandidateRepository
{
    private readonly CandidatesDbContext _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Candidate>> ListAsync(
        string? city, int skip, int take, CancellationToken cancellationToken)
    {
        var query = _db.Candidates.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(city))
        {
            // Case-insensitive match translated to PostgreSQL ILIKE (avoids client-side ToLower).
            query = query.Where(c => EF.Functions.ILike(c.City, city));
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Candidate?> GetByIdAsync(CandidateId id, CancellationToken cancellationToken)
        => await _db.Candidates.FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddPool(TalentPool pool) => _db.TalentPools.Add(pool);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TalentPool>> ListPoolsAsync(CancellationToken cancellationToken)
        => await _db.TalentPools.AsNoTracking().OrderByDescending(p => p.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<TalentPool?> GetPoolAsync(TalentPoolId id, CancellationToken cancellationToken)
        => await _db.TalentPools.FirstOrDefaultAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddPoolMember(TalentPoolMember member) => _db.TalentPoolMembers.Add(member);

    /// <inheritdoc />
    public void RemovePoolMember(TalentPoolMember member) => _db.TalentPoolMembers.Remove(member);

    /// <inheritdoc />
    public async Task<TalentPoolMember?> GetPoolMemberAsync(TalentPoolId poolId, CandidateId candidateId, CancellationToken cancellationToken)
        => await _db.TalentPoolMembers.FirstOrDefaultAsync(m => m.PoolId == poolId && m.CandidateId == candidateId, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TalentPoolMember>> ListPoolMembersAsync(TalentPoolId poolId, CancellationToken cancellationToken)
        => await _db.TalentPoolMembers.AsNoTracking().Where(m => m.PoolId == poolId).OrderByDescending(m => m.AddedAt).ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void Add(Candidate candidate) => _db.Candidates.Add(candidate);

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<CandidateStatsDto> GetStatsAsync(CancellationToken cancellationToken)
    {
        var set = _db.Candidates.AsNoTracking();

        var total = await set.CountAsync(cancellationToken).ConfigureAwait(false);

        var cityRaw = await set
            .GroupBy(c => c.City)
            .Select(g => new { City = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(12)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var availabilityRaw = await set
            .GroupBy(c => c.Availability)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byCity = cityRaw.Select(x => new CountByLabel(x.City, x.Count)).ToList();
        var byAvailability = availabilityRaw
            .Select(x => new CountByLabel(x.Status.ToString(), x.Count))
            .OrderByDescending(x => x.Count)
            .ToList();

        return new CandidateStatsDto(total, byCity, byAvailability);
    }
}
