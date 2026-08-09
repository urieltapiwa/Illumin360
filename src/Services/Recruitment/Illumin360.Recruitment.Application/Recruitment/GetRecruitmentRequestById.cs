using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>Query: fetch a single recruitment request by id.</summary>
/// <param name="Id">The request id.</param>
public sealed record GetRecruitmentRequestByIdQuery(Guid Id) : IQuery<RecruitmentRequestDto>;

/// <summary>Handles <see cref="GetRecruitmentRequestByIdQuery"/> by reading from the recruitment repository.</summary>
public sealed class GetRecruitmentRequestByIdQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetRecruitmentRequestByIdQuery, RecruitmentRequestDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<RecruitmentRequestDto>> HandleAsync(
        GetRecruitmentRequestByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var request = await _repository
            .GetByIdAsync(new RequestId(query.Id), cancellationToken)
            .ConfigureAwait(false);

        if (request is null)
        {
            return Error.NotFound("recruitment_request.not_found", $"No recruitment request found with id '{query.Id}'.");
        }

        return RecruitmentRequestDto.FromDomain(request);
    }
}
