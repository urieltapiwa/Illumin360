using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Professionals.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IProfessionalRepository"/>.</summary>
/// <param name="db">The Professionals database context.</param>
public sealed class ProfessionalRepository(ProfessionalsDbContext db) : IProfessionalRepository
{
    private readonly ProfessionalsDbContext _db = db;

    /// <inheritdoc />
    public async Task<ProfessionalDashboard?> GetDashboardAsync(ProfessionalId id, CancellationToken cancellationToken)
    {
        var professional = await _db.Professionals.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);

        return professional is null ? null : await LoadAsync(professional, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ProfessionalDashboard?> GetDefaultDashboardAsync(CancellationToken cancellationToken)
    {
        var professional = await _db.Professionals.AsNoTracking()
            .OrderBy(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return professional is null ? null : await LoadAsync(professional, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Add(Professional professional) => _db.Professionals.Add(professional);

    /// <inheritdoc />
    public async Task<ProfessionalId?> GetDefaultProfessionalIdAsync(CancellationToken cancellationToken)
    {
        var p = await _db.Professionals.AsNoTracking().OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return p?.Id;
    }

    /// <inheritdoc />
    public Task<Professional?> GetTrackedAsync(ProfessionalId id, CancellationToken cancellationToken) =>
        _db.Professionals.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<ProfessionalMatch?> GetMatchAsync(ProfessionalId professionalId, Guid matchId, CancellationToken cancellationToken) =>
        _db.Matches.FirstOrDefaultAsync(m => m.Id == matchId && m.ProfessionalId == professionalId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetSkillNamesAsync(ProfessionalId professionalId, CancellationToken cancellationToken) =>
        await _db.Skills.AsNoTracking()
            .Where(s => s.ProfessionalId == professionalId)
            .Select(s => s.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddSkill(ProfessionalSkill skill) => _db.Skills.Add(skill);

    /// <inheritdoc />
    public void AddNotification(ProfessionalNotification notification) => _db.Notifications.Add(notification);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProfessionalNotification>> ListNotificationsAsync(ProfessionalId professionalId, bool unreadOnly, CancellationToken cancellationToken)
    {
        var query = _db.Notifications.AsNoTracking().Where(n => n.ProfessionalId == professionalId);
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query.OrderByDescending(n => n.CreatedAt).Take(50).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<ProfessionalNotification?> GetNotificationAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<int> MarkAllNotificationsReadAsync(ProfessionalId professionalId, CancellationToken cancellationToken)
    {
        var unread = await _db.Notifications.Where(n => n.ProfessionalId == professionalId && !n.IsRead).ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var n in unread)
        {
            n.MarkRead();
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return unread.Count;
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);

    private async Task<ProfessionalDashboard> LoadAsync(Professional professional, CancellationToken cancellationToken)
    {
        var id = professional.Id;

        var matches = await _db.Matches.AsNoTracking()
            .Where(x => x.ProfessionalId == id).OrderBy(x => x.Sort)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var pipeline = await _db.Pipeline.AsNoTracking()
            .Where(x => x.ProfessionalId == id).OrderBy(x => x.Sort)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var skillDemand = await _db.SkillDemand.AsNoTracking()
            .Where(x => x.ProfessionalId == id).OrderBy(x => x.Sort)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var skills = await _db.Skills.AsNoTracking()
            .Where(x => x.ProfessionalId == id).OrderBy(x => x.Sort)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var activity = await _db.Activity.AsNoTracking()
            .Where(x => x.ProfessionalId == id).OrderBy(x => x.Sort)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new ProfessionalDashboard(professional, matches, pipeline, skillDemand, skills, activity);
    }
}
