using Illumin360.Employers.Application.Abstractions;
using Illumin360.Employers.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Employers.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IEmployerRepository"/>.</summary>
/// <param name="db">The Employers database context.</param>
public sealed class EmployerRepository(EmployersDbContext db) : IEmployerRepository
{
    private readonly EmployersDbContext _db = db;

    /// <inheritdoc />
    public async Task<Employer?> GetDefaultAsync(CancellationToken cancellationToken)
        => await _db.Employers.AsNoTracking().OrderBy(e => e.CreatedAt).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Employer?> GetDefaultTrackedAsync(CancellationToken cancellationToken)
        => await _db.Employers.OrderBy(e => e.CreatedAt).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Employer?> GetByIdAsync(EmployerId id, CancellationToken cancellationToken)
        => await _db.Employers.FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void Add(Employer employer) => _db.Employers.Add(employer);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
