using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>The active filter set for a candidate search (all optional / AND-combined).</summary>
/// <param name="City">Exact city match (case-insensitive), if set.</param>
/// <param name="Availability">Availability status, if set.</param>
/// <param name="Query">Keyword matched against name and headline, if set.</param>
/// <param name="HasCv">Whether the candidate has (true) or lacks (false) a CV, if set.</param>
public sealed record CandidateSearchCriteria(string? City, AvailabilityStatus? Availability, string? Query, bool? HasCv);

/// <summary>Facet counts for a candidate search — each dimension excludes its own active filter.</summary>
/// <param name="Cities">Top cities and their counts.</param>
/// <param name="Availability">Availability statuses and their counts.</param>
public sealed record CandidateFacetsDto(IReadOnlyList<CountByLabel> Cities, IReadOnlyList<CountByLabel> Availability);

/// <summary>A page of search results plus the facet counts for refining the search.</summary>
/// <param name="Items">The candidates on this page.</param>
/// <param name="Total">Total candidates matching the filters.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Facets">Facet counts for the current filter set.</param>
public sealed record CandidateSearchResultDto(IReadOnlyList<CandidateDto> Items, int Total, int Page, int PageSize, CandidateFacetsDto Facets);

/// <summary>Query: faceted candidate search over city, availability, keyword and CV presence.</summary>
/// <param name="City">Optional city filter.</param>
/// <param name="Availability">Optional availability filter (enum name).</param>
/// <param name="Query">Optional keyword (name / headline).</param>
/// <param name="HasCv">Optional CV-presence filter.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size (1–100).</param>
public sealed record SearchCandidatesQuery(string? City, string? Availability, string? Query, bool? HasCv, int Page = 1, int PageSize = 20)
    : IQuery<CandidateSearchResultDto>;

/// <summary>Handles <see cref="SearchCandidatesQuery"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class SearchCandidatesQueryHandler(ICandidateRepository repository)
    : IQueryHandler<SearchCandidatesQuery, CandidateSearchResultDto>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<CandidateSearchResultDto>> HandleAsync(SearchCandidatesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        AvailabilityStatus? availability = null;
        if (!string.IsNullOrWhiteSpace(query.Availability))
        {
            if (!Enum.TryParse<AvailabilityStatus>(query.Availability, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            {
                return Error.Validation("candidate.availability_invalid", "Availability must be one of ActivelyLooking, OpenToOpportunities or NotAvailable.");
            }

            availability = parsed;
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var criteria = new CandidateSearchCriteria(
            string.IsNullOrWhiteSpace(query.City) ? null : query.City.Trim(),
            availability,
            string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim(),
            query.HasCv);

        var (items, total) = await _repository.SearchAsync(criteria, (page - 1) * pageSize, pageSize, cancellationToken).ConfigureAwait(false);
        var facets = await _repository.GetCandidateFacetsAsync(criteria, cancellationToken).ConfigureAwait(false);

        return new CandidateSearchResultDto(items.Select(CandidateDto.FromDomain).ToList(), total, page, pageSize, facets);
    }
}
