using Illumin360.Admin.Domain;

namespace Illumin360.Admin.Application.Abstractions;

/// <summary>Persistence port for the admin account directory.</summary>
public interface IAccountRepository
{
    /// <summary>Lists accounts, optionally filtered by status, by name.</summary>
    /// <param name="status">Optional status filter (active/suspended); null for all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching accounts.</returns>
    Task<IReadOnlyList<AdminAccount>> ListAsync(string? status, CancellationToken cancellationToken);

    /// <summary>Fetches a single account by id.</summary>
    /// <param name="id">The account id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account, or <see langword="null"/> if not found.</returns>
    Task<AdminAccount?> GetByIdAsync(AccountId id, CancellationToken cancellationToken);

    /// <summary>Commits pending changes (and flushes the outbox in the same transaction).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
