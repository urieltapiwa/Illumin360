using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>Per-role careers-page view analytics.</summary>
/// <param name="RequestId">The requisition.</param>
/// <param name="Title">Role title.</param>
/// <param name="City">Role city.</param>
/// <param name="Views">Total detail-page views.</param>
/// <param name="LastViewedAt">When last viewed (UTC), if ever.</param>
public sealed record CareerViewDto(Guid RequestId, string Title, string City, long Views, DateTimeOffset? LastViewedAt);

/// <summary>Records a careers detail-page view for a role (fire-and-forget from the SSR page).</summary>
/// <param name="RequestId">The requisition id.</param>
public sealed record RecordCareerViewCommand(Guid RequestId) : ICommand<bool>;

/// <summary>Per-role careers view counts (descending), joined to role titles.</summary>
public sealed record GetCareerViewsQuery : IQuery<IReadOnlyList<CareerViewDto>>;

/// <summary>Handles <see cref="RecordCareerViewCommand"/> — upserts + increments the role's view counter.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class RecordCareerViewCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<RecordCareerViewCommand, bool>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(RecordCareerViewCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.RequestId == Guid.Empty)
        {
            return false;
        }

        await _repository.RecordCareerViewAsync(command.RequestId, DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
        return true;
    }
}

/// <summary>Handles <see cref="GetCareerViewsQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetCareerViewsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetCareerViewsQuery, IReadOnlyList<CareerViewDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CareerViewDto>>> HandleAsync(GetCareerViewsQuery query, CancellationToken cancellationToken)
    {
        var views = await _repository.GetCareerViewsAsync(cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<CareerViewDto>>.Success(views);
    }
}
