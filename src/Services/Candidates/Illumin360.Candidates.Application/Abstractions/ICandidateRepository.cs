using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;

namespace Illumin360.Candidates.Application.Abstractions;

/// <summary>
/// Port for candidate persistence. Implemented by the Infrastructure layer
/// (ports &amp; adapters — charter Part 5).
/// </summary>
public interface ICandidateRepository
{
    /// <summary>Returns a page of candidates, optionally filtered by city.</summary>
    /// <param name="city">Optional city filter (case-insensitive).</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching candidates.</returns>
    Task<IReadOnlyList<Candidate>> ListAsync(string? city, int skip, int take, CancellationToken cancellationToken);

    /// <summary>Finds a candidate by id, or null if not present.</summary>
    /// <param name="id">The candidate id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Candidate?> GetByIdAsync(CandidateId id, CancellationToken cancellationToken);

    /// <summary>Stages a new candidate for insertion. Persisted on <see cref="SaveChangesAsync"/>.</summary>
    /// <param name="candidate">The candidate to add.</param>
    void Add(Candidate candidate);

    /// <summary>Commits staged changes to the data store.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Returns aggregate statistics over the candidate pool (total + city/availability breakdowns).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregate candidate statistics.</returns>
    Task<CandidateStatsDto> GetStatsAsync(CancellationToken cancellationToken);
}
