using Illumin360.Candidates.Application.Abstractions;
using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>Query: list candidates with optional city filter and cursor-free paging.</summary>
/// <param name="City">Optional city filter.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size (1–100).</param>
public sealed record GetCandidatesQuery(string? City, int Page = 1, int PageSize = 20)
    : IQuery<IReadOnlyList<CandidateDto>>;

/// <summary>Handles <see cref="GetCandidatesQuery"/> by reading from the candidate repository.</summary>
public sealed class GetCandidatesQueryHandler(ICandidateRepository repository)
    : IQueryHandler<GetCandidatesQuery, IReadOnlyList<CandidateDto>>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CandidateDto>>> HandleAsync(
        GetCandidatesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var candidates = await _repository
            .ListAsync(query.City, (page - 1) * pageSize, pageSize, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CandidateDto> dtos = candidates.Select(CandidateDto.FromDomain).ToList();
        return Result<IReadOnlyList<CandidateDto>>.Success(dtos);
    }
}
