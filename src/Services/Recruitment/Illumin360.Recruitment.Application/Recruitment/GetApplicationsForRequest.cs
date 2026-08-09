using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>Query: list applications for a recruitment request, highest match score first.</summary>
/// <param name="RequestId">The request id.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size (1–200).</param>
public sealed record GetApplicationsForRequestQuery(Guid RequestId, int Page = 1, int PageSize = 50)
    : IQuery<IReadOnlyList<ApplicationDto>>;

/// <summary>Handles <see cref="GetApplicationsForRequestQuery"/> by reading from the recruitment repository.</summary>
public sealed class GetApplicationsForRequestQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetApplicationsForRequestQuery, IReadOnlyList<ApplicationDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ApplicationDto>>> HandleAsync(
        GetApplicationsForRequestQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var apps = await _repository
            .ListApplicationsAsync(new RequestId(query.RequestId), (page - 1) * pageSize, pageSize, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ApplicationDto> dtos = apps.Select(ApplicationDto.FromDomain).ToList();
        return Result<IReadOnlyList<ApplicationDto>>.Success(dtos);
    }
}
