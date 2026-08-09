using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Admin.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IVerificationRepository"/>.</summary>
/// <param name="db">The Admin database context.</param>
public sealed class VerificationRepository(AdminDbContext db) : IVerificationRepository
{
    private readonly AdminDbContext _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Verification>> ListAsync(string? status, CancellationToken cancellationToken)
    {
        var query = _db.Verifications.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<VerificationStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(v => v.Status == parsed);
        }

        return await query.OrderBy(v => v.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Verification?> GetByIdAsync(VerificationId id, CancellationToken cancellationToken) =>
        _db.Verifications.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);
}
