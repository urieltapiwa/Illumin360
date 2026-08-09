using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Admin.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="ITicketRepository"/>.</summary>
/// <param name="db">The Admin database context.</param>
public sealed class TicketRepository(AdminDbContext db) : ITicketRepository
{
    private readonly AdminDbContext _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Ticket>> ListAsync(string? status, CancellationToken cancellationToken)
    {
        var query = _db.Tickets.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TicketStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(t => t.Status == parsed);
        }

        return await query.OrderBy(t => t.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Ticket?> GetByIdAsync(TicketId id, CancellationToken cancellationToken) =>
        _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
