using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Microsoft.EntityFrameworkCore;
using ApplicationId = Illumin360.Recruitment.Domain.ApplicationId;

namespace Illumin360.Recruitment.Infrastructure.Persistence;

/// <summary>EF Core adapter implementing <see cref="IRecruitmentRepository"/>.</summary>
public sealed class RecruitmentRepository(RecruitmentDbContext db) : IRecruitmentRepository
{
    // Funnel stage ordering for presentation (applied → reviewed → shortlisted → hired → rejected).
    private static readonly string[] StageOrder = ["applied", "reviewed", "shortlisted", "hired", "rejected"];

    private readonly RecruitmentDbContext _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecruitmentRequest>> ListAsync(
        string? city, string? status, int skip, int take, CancellationToken cancellationToken)
    {
        var query = _db.Requests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(city))
        {
            // Case-insensitive match translated to PostgreSQL ILIKE (avoids client-side ToLower).
            query = query.Where(r => EF.Functions.ILike(r.City, city));
        }

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<RequestStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(r => r.Status == parsedStatus);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RecruitmentRequest?> GetByIdAsync(RequestId id, CancellationToken cancellationToken)
        => await _db.Requests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecruitmentApplication>> ListApplicationsAsync(
        RequestId requestId, int skip, int take, CancellationToken cancellationToken)
        => await _db.Applications.AsNoTracking()
            .Where(a => a.RequestId == requestId)
            .OrderByDescending(a => a.MatchScore)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void Add(RecruitmentRequest request) => _db.Requests.Add(request);

    /// <inheritdoc />
    public async Task<bool> HasApplicationAsync(RequestId requestId, Guid talentId, CancellationToken cancellationToken)
        => await _db.Applications.AsNoTracking()
            .AnyAsync(a => a.RequestId == requestId && a.TalentId == talentId, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void AddApplication(RecruitmentApplication application) => _db.Applications.Add(application);

    /// <inheritdoc />
    public async Task<RecruitmentApplication?> GetApplicationAsync(ApplicationId id, CancellationToken cancellationToken)
        => await _db.Applications.FirstOrDefaultAsync(a => a.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecruitmentApplication>> ListApplicationsForTalentAsync(Guid talentId, int skip, int take, CancellationToken cancellationToken)
        => await _db.Applications.AsNoTracking()
            .Where(a => a.TalentId == talentId)
            .OrderByDescending(a => a.AppliedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<RecruitmentStatsDto> GetStatsAsync(CancellationToken cancellationToken)
    {
        var requests = _db.Requests.AsNoTracking();
        var apps = _db.Applications.AsNoTracking();

        var totalRequests = await requests.CountAsync(cancellationToken).ConfigureAwait(false);
        var filledRequests = await requests
            .CountAsync(r => r.Status == RequestStatus.Filled, cancellationToken).ConfigureAwait(false);
        var openRequests = await requests
            .CountAsync(r => r.Status == RequestStatus.Open, cancellationToken).ConfigureAwait(false);

        var totalApplications = await apps.CountAsync(cancellationToken).ConfigureAwait(false);
        var totalHires = await apps.CountAsync(a => a.IsHire, cancellationToken).ConfigureAwait(false);
        var avgScore = totalApplications == 0
            ? 0.0
            : (double)await apps.AverageAsync(a => a.MatchScore, cancellationToken).ConfigureAwait(false);

        var funnelRaw = await apps
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byTypeRaw = await apps
            .GroupBy(a => a.TalentType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var cityRaw = await requests
            .GroupBy(r => r.City)
            .Select(g => new { City = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(12)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var trendRaw = await apps
            .GroupBy(a => new { a.AppliedAt.Year, a.AppliedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Applications = g.Count(),
                Hires = g.Sum(a => a.IsHire ? 1 : 0),
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var funnel = funnelRaw
            .OrderBy(x => Array.IndexOf(StageOrder, x.Status))
            .Select(x => new CountByLabel(x.Status, x.Count))
            .ToList();

        var byTalentType = byTypeRaw
            .Select(x => new CountByLabel(x.Type, x.Count))
            .OrderByDescending(x => x.Count)
            .ToList();

        var topCities = cityRaw.Select(x => new CountByLabel(x.City, x.Count)).ToList();

        var trend = trendRaw
            .Select(x => new MonthlyPoint($"{x.Year:D4}-{x.Month:D2}", x.Applications, x.Hires))
            .ToList();

        return new RecruitmentStatsDto(
            totalRequests,
            openRequests,
            filledRequests,
            totalApplications,
            totalHires,
            Math.Round(avgScore, 1),
            funnel,
            byTalentType,
            topCities,
            trend);
    }
}
