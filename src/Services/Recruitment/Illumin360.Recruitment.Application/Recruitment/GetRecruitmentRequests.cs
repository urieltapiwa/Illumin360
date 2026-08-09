using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>Query: list recruitment requests with optional city/status filter and paging.</summary>
/// <param name="City">Optional city filter.</param>
/// <param name="Status">Optional status filter (<c>open</c>/<c>filled</c>/<c>closed</c>).</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size (1–100).</param>
public sealed record GetRecruitmentRequestsQuery(string? City, string? Status, int Page = 1, int PageSize = 20)
    : IQuery<IReadOnlyList<RecruitmentRequestDto>>;

/// <summary>Handles <see cref="GetRecruitmentRequestsQuery"/> by reading from the recruitment repository.</summary>
public sealed class GetRecruitmentRequestsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetRecruitmentRequestsQuery, IReadOnlyList<RecruitmentRequestDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RecruitmentRequestDto>>> HandleAsync(
        GetRecruitmentRequestsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var requests = await _repository
            .ListAsync(query.City, query.Status, (page - 1) * pageSize, pageSize, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<RecruitmentRequestDto> dtos = requests.Select(RecruitmentRequestDto.FromDomain).ToList();
        return Result<IReadOnlyList<RecruitmentRequestDto>>.Success(dtos);
    }
}
