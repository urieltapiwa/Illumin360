using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Admin.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IAccountRepository"/>.</summary>
/// <param name="db">The Admin database context.</param>
public sealed class AccountRepository(AdminDbContext db) : IAccountRepository
{
    private readonly AdminDbContext _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminAccount>> ListAsync(string? status, CancellationToken cancellationToken)
    {
        var query = _db.Accounts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AccountStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(a => a.Status == parsed);
        }

        return await query.OrderBy(a => a.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<AdminAccount?> GetByIdAsync(AccountId id, CancellationToken cancellationToken) =>
        _db.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
