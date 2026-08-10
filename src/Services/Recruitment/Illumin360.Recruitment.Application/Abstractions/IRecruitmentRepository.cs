using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;

namespace Illumin360.Recruitment.Application.Abstractions;

/// <summary>
/// Port for recruitment persistence. Implemented by the Infrastructure layer
/// (ports &amp; adapters — charter Part 5).
/// </summary>
public interface IRecruitmentRepository
{
    /// <summary>Returns a page of recruitment requests, optionally filtered by city and/or status.</summary>
    /// <param name="city">Optional city filter (case-insensitive).</param>
    /// <param name="status">Optional status filter (<c>open</c>/<c>filled</c>/<c>closed</c>, case-insensitive).</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching requests.</returns>
    Task<IReadOnlyList<RecruitmentRequest>> ListAsync(
        string? city, string? status, int skip, int take, CancellationToken cancellationToken);

    /// <summary>Finds a request by id, or null if not present.</summary>
    /// <param name="id">The request id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RecruitmentRequest?> GetByIdAsync(RequestId id, CancellationToken cancellationToken);

    /// <summary>Returns a page of applications for a request, highest match score first.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<RecruitmentApplication>> ListApplicationsAsync(
        RequestId requestId, int skip, int take, CancellationToken cancellationToken);

    /// <summary>Stages a new request for insertion. Persisted on <see cref="SaveChangesAsync"/>.</summary>
    /// <param name="request">The request to add.</param>
    void Add(RecruitmentRequest request);

    /// <summary>Whether the given talent already has an application against the request.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="talentId">The talent id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> HasApplicationAsync(RequestId requestId, Guid talentId, CancellationToken cancellationToken);

    /// <summary>Stages a new application for insertion. Persisted on <see cref="SaveChangesAsync"/>.</summary>
    /// <param name="application">The application to add.</param>
    void AddApplication(RecruitmentApplication application);

    /// <summary>Commits staged changes to the data store.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Returns aggregate recruitment statistics (funnel, hires trend, matching, top cities).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregate recruitment statistics.</returns>
    Task<RecruitmentStatsDto> GetStatsAsync(CancellationToken cancellationToken);
}
