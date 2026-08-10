using Illumin360.Employers.Domain;

namespace Illumin360.Employers.Application.Abstractions;

/// <summary>Persistence port for the Employers bounded context.</summary>
public interface IEmployerRepository
{
    /// <summary>Loads the default ("me") employer (most recently created), or null.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Employer?> GetDefaultAsync(CancellationToken cancellationToken);

    /// <summary>Loads the default ("me") employer for update (change-tracked), or null.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Employer?> GetDefaultTrackedAsync(CancellationToken cancellationToken);

    /// <summary>Loads an employer by id, or null.</summary>
    /// <param name="id">The employer id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Employer?> GetByIdAsync(EmployerId id, CancellationToken cancellationToken);

    /// <summary>Stages a new employer for insertion.</summary>
    /// <param name="employer">The employer to add.</param>
    void Add(Employer employer);

    /// <summary>Commits staged changes.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
