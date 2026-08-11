using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Admin.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IAuditRepository"/>.</summary>
/// <param name="db">The Admin database context.</param>
public sealed class AuditRepository(AdminDbContext db) : IAuditRepository
{
    private readonly AdminDbContext _db = db;

    /// <inheritdoc />
    public void Add(AuditEntry entry) => _db.AuditEntries.Add(entry);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditEntry>> ListAsync(string? action, int skip, int take, CancellationToken cancellationToken)
    {
        var query = _db.AuditEntries.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(e => EF.Functions.ILike(e.Action, action + "%"));
        }

        return await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
