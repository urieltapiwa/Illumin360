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

    /// <summary>Faceted search: a page of candidates matching the criteria, plus the total match count.</summary>
    /// <param name="criteria">The active filter set.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page of matches and the total count.</returns>
    Task<(IReadOnlyList<Candidate> Items, int Total)> SearchAsync(CandidateSearchCriteria criteria, int skip, int take, CancellationToken cancellationToken);

    /// <summary>Facet counts for a search — each facet excludes its own active filter.</summary>
    /// <param name="criteria">The active filter set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>City and availability facet counts.</returns>
    Task<CandidateFacetsDto> GetCandidateFacetsAsync(CandidateSearchCriteria criteria, CancellationToken cancellationToken);

    /// <summary>Finds a candidate by id, or null if not present.</summary>
    /// <param name="id">The candidate id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Candidate?> GetByIdAsync(CandidateId id, CancellationToken cancellationToken);

    /// <summary>Stages a new candidate for insertion. Persisted on <see cref="SaveChangesAsync"/>.</summary>
    /// <param name="candidate">The candidate to add.</param>
    void Add(Candidate candidate);

    /// <summary>
    /// Right-to-be-forgotten erase: permanently deletes the candidate and all their owned data (notes,
    /// tags, pool memberships) in one operation.
    /// </summary>
    /// <param name="id">The candidate id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EraseCandidateAsync(CandidateId id, CancellationToken cancellationToken);

    /// <summary>Stages a new recruiter note for insertion.</summary>
    /// <param name="note">The note to add.</param>
    void AddNote(CandidateNote note);

    /// <summary>Removes a recruiter note.</summary>
    /// <param name="note">The note to remove.</param>
    void RemoveNote(CandidateNote note);

    /// <summary>Loads a recruiter note by id, or null if not present.</summary>
    /// <param name="id">The note id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<CandidateNote?> GetNoteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lists a candidate's recruiter notes, newest first.</summary>
    /// <param name="candidateId">The candidate id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<CandidateNote>> ListNotesAsync(CandidateId candidateId, CancellationToken cancellationToken);

    /// <summary>Stages a new tag for insertion.</summary>
    /// <param name="tag">The tag to add.</param>
    void AddTag(CandidateTag tag);

    /// <summary>Removes a tag.</summary>
    /// <param name="tag">The tag to remove.</param>
    void RemoveTag(CandidateTag tag);

    /// <summary>Whether a candidate already has the given (normalised) tag label.</summary>
    /// <param name="candidateId">The candidate id.</param>
    /// <param name="label">The normalised tag label.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> TagExistsAsync(CandidateId candidateId, string label, CancellationToken cancellationToken);

    /// <summary>Loads a candidate's tag by normalised label, or null if not present.</summary>
    /// <param name="candidateId">The candidate id.</param>
    /// <param name="label">The normalised tag label.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<CandidateTag?> GetTagAsync(CandidateId candidateId, string label, CancellationToken cancellationToken);

    /// <summary>Lists a candidate's tags, alphabetically.</summary>
    /// <param name="candidateId">The candidate id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<CandidateTag>> ListTagsAsync(CandidateId candidateId, CancellationToken cancellationToken);

    /// <summary>Commits staged changes to the data store.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Returns aggregate statistics over the candidate pool (total + city/availability breakdowns).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregate candidate statistics.</returns>
    Task<CandidateStatsDto> GetStatsAsync(CancellationToken cancellationToken);

    /// <summary>Returns an anonymised diversity report (counts by nationality, city and availability).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The diversity report.</returns>
    Task<DiversityReportDto> GetDiversityReportAsync(CancellationToken cancellationToken);

    /// <summary>Stages a new talent pool for insertion.</summary>
    /// <param name="pool">The pool to add.</param>
    void AddPool(TalentPool pool);

    /// <summary>Lists all talent pools, newest first.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<TalentPool>> ListPoolsAsync(CancellationToken cancellationToken);

    /// <summary>Loads a talent pool by id, or null if not present.</summary>
    /// <param name="id">The pool id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TalentPool?> GetPoolAsync(TalentPoolId id, CancellationToken cancellationToken);

    /// <summary>Stages a new pool membership for insertion.</summary>
    /// <param name="member">The membership to add.</param>
    void AddPoolMember(TalentPoolMember member);

    /// <summary>Removes a pool membership.</summary>
    /// <param name="member">The membership to remove.</param>
    void RemovePoolMember(TalentPoolMember member);

    /// <summary>Finds a candidate's membership in a pool, or null.</summary>
    /// <param name="poolId">The pool id.</param>
    /// <param name="candidateId">The candidate id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TalentPoolMember?> GetPoolMemberAsync(TalentPoolId poolId, CandidateId candidateId, CancellationToken cancellationToken);

    /// <summary>Lists a pool's memberships.</summary>
    /// <param name="poolId">The pool id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<TalentPoolMember>> ListPoolMembersAsync(TalentPoolId poolId, CancellationToken cancellationToken);
}
