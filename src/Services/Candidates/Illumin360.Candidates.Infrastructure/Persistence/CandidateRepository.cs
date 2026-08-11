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
    public async Task<IReadOnlyList<Candidate>> ListAllAsync(CancellationToken cancellationToken)
        => await _db.Candidates.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Candidate> Items, int Total)> SearchAsync(
        CandidateSearchCriteria criteria, int skip, int take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        var query = Apply(_db.Candidates.AsNoTracking(), criteria, applyCity: true, applyAvailability: true);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<CandidateFacetsDto> GetCandidateFacetsAsync(CandidateSearchCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        // Each facet excludes its own dimension so the counts show what selecting that value would yield.
        var cityRaw = await Apply(_db.Candidates.AsNoTracking(), criteria, applyCity: false, applyAvailability: true)
            .GroupBy(c => c.City)
            .Select(g => new { City = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(12)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var availabilityRaw = await Apply(_db.Candidates.AsNoTracking(), criteria, applyCity: true, applyAvailability: false)
            .GroupBy(c => c.Availability)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var cities = cityRaw.Select(x => new CountByLabel(x.City, x.Count)).ToList();
        var availability = availabilityRaw
            .Select(x => new CountByLabel(x.Status.ToString(), x.Count))
            .OrderByDescending(x => x.Count)
            .ToList();

        return new CandidateFacetsDto(cities, availability);
    }

    private static IQueryable<Candidate> Apply(IQueryable<Candidate> query, CandidateSearchCriteria c, bool applyCity, bool applyAvailability)
    {
        if (applyCity && !string.IsNullOrWhiteSpace(c.City))
        {
            query = query.Where(x => EF.Functions.ILike(x.City, c.City));
        }

        if (applyAvailability && c.Availability is { } availability)
        {
            query = query.Where(x => x.Availability == availability);
        }

        if (!string.IsNullOrWhiteSpace(c.Query))
        {
            var keyword = $"%{c.Query}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.FirstName, keyword)
                || EF.Functions.ILike(x.LastName, keyword)
                || (x.PublicHeadline != null && EF.Functions.ILike(x.PublicHeadline, keyword)));
        }

        if (c.HasCv is { } hasCv)
        {
            query = query.Where(x => (x.CvObjectKey != null) == hasCv);
        }

        return query;
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
    public void AddNote(CandidateNote note) => _db.CandidateNotes.Add(note);

    /// <inheritdoc />
    public void RemoveNote(CandidateNote note) => _db.CandidateNotes.Remove(note);

    /// <inheritdoc />
    public async Task<CandidateNote?> GetNoteAsync(Guid id, CancellationToken cancellationToken)
        => await _db.CandidateNotes.FirstOrDefaultAsync(n => n.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CandidateNote>> ListNotesAsync(CandidateId candidateId, CancellationToken cancellationToken)
        => await _db.CandidateNotes.AsNoTracking()
            .Where(n => n.CandidateId == candidateId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddTag(CandidateTag tag) => _db.CandidateTags.Add(tag);

    /// <inheritdoc />
    public void RemoveTag(CandidateTag tag) => _db.CandidateTags.Remove(tag);

    /// <inheritdoc />
    public async Task<bool> TagExistsAsync(CandidateId candidateId, string label, CancellationToken cancellationToken)
        => await _db.CandidateTags.AsNoTracking()
            .AnyAsync(t => t.CandidateId == candidateId && t.Label == label, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<CandidateTag?> GetTagAsync(CandidateId candidateId, string label, CancellationToken cancellationToken)
        => await _db.CandidateTags.FirstOrDefaultAsync(t => t.CandidateId == candidateId && t.Label == label, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CandidateTag>> ListTagsAsync(CandidateId candidateId, CancellationToken cancellationToken)
        => await _db.CandidateTags.AsNoTracking()
            .Where(t => t.CandidateId == candidateId)
            .OrderBy(t => t.Label)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void Add(Candidate candidate) => _db.Candidates.Add(candidate);

    /// <inheritdoc />
    public async Task EraseCandidateAsync(CandidateId id, CancellationToken cancellationToken)
    {
        // Delete owned rows first, then the candidate. ExecuteDelete runs immediately (its own statement).
        await _db.TalentPoolMembers.Where(m => m.CandidateId == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _db.CandidateTags.Where(t => t.CandidateId == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _db.CandidateNotes.Where(n => n.CandidateId == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _db.Candidates.Where(c => c.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddCustomField(CustomFieldDefinition field) => _db.CustomFieldDefinitions.Add(field);

    /// <inheritdoc />
    public void RemoveCustomField(CustomFieldDefinition field) => _db.CustomFieldDefinitions.Remove(field);

    /// <inheritdoc />
    public async Task<CustomFieldDefinition?> GetCustomFieldAsync(Guid id, CancellationToken cancellationToken)
        => await _db.CustomFieldDefinitions.FirstOrDefaultAsync(f => f.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CustomFieldDefinition>> ListCustomFieldsAsync(CancellationToken cancellationToken)
        => await _db.CustomFieldDefinitions.AsNoTracking()
            .OrderBy(f => f.SortOrder)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddCandidateValue(CandidateCustomValue value) => _db.CandidateCustomValues.Add(value);

    /// <inheritdoc />
    public void RemoveCandidateValue(CandidateCustomValue value) => _db.CandidateCustomValues.Remove(value);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CandidateCustomValue>> ListCandidateValuesAsync(Guid candidateId, CancellationToken cancellationToken)
        => await _db.CandidateCustomValues.AsNoTracking()
            .Where(v => v.CandidateId == candidateId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CandidateCustomValue>> ListCandidateValuesTrackedAsync(Guid candidateId, CancellationToken cancellationToken)
        => await _db.CandidateCustomValues
            .Where(v => v.CandidateId == candidateId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<DiversityReportDto> GetDiversityReportAsync(CancellationToken cancellationToken)
    {
        var set = _db.Candidates.AsNoTracking();

        var total = await set.CountAsync(cancellationToken).ConfigureAwait(false);

        var nationalityRaw = await set
            .GroupBy(c => c.Nationality)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var cityRaw = await set
            .GroupBy(c => c.City)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var availabilityRaw = await set
            .GroupBy(c => c.Availability)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byNationality = nationalityRaw.Select(x => new CountByLabel(x.Label, x.Count)).ToList();
        var byCity = cityRaw.Select(x => new CountByLabel(x.Label, x.Count)).ToList();
        var byAvailability = availabilityRaw
            .Select(x => new CountByLabel(x.Status.ToString(), x.Count))
            .OrderByDescending(x => x.Count)
            .ToList();

        return new DiversityReportDto(total, byNationality, byCity, byAvailability);
    }

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
