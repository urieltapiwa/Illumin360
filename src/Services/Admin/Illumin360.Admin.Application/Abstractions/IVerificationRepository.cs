using Illumin360.Admin.Domain;

namespace Illumin360.Admin.Application.Abstractions;

/// <summary>Persistence port for the verification queue.</summary>
public interface IVerificationRepository
{
    /// <summary>Lists verifications, optionally filtered by status, newest first.</summary>
    /// <param name="status">Optional status filter (pending/approved/rejected); null for all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching verifications.</returns>
    Task<IReadOnlyList<Verification>> ListAsync(string? status, CancellationToken cancellationToken);

    /// <summary>Fetches a single verification by id.</summary>
    /// <param name="id">The verification id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The verification, or <see langword="null"/> if not found.</returns>
    Task<Verification?> GetByIdAsync(VerificationId id, CancellationToken cancellationToken);

    /// <summary>Commits pending changes (and flushes the outbox in the same transaction).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
