using Illumin360.Admin.Domain;

namespace Illumin360.Admin.Application.Abstractions;

/// <summary>Persistence port for support tickets.</summary>
public interface ITicketRepository
{
    /// <summary>Lists tickets, optionally filtered by status, newest first.</summary>
    /// <param name="status">Optional status filter (open/assigned/resolved); null for all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching tickets.</returns>
    Task<IReadOnlyList<Ticket>> ListAsync(string? status, CancellationToken cancellationToken);

    /// <summary>Fetches a single ticket by id.</summary>
    /// <param name="id">The ticket id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ticket, or <see langword="null"/> if not found.</returns>
    Task<Ticket?> GetByIdAsync(TicketId id, CancellationToken cancellationToken);

    /// <summary>Commits pending changes (and flushes the outbox in the same transaction).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
