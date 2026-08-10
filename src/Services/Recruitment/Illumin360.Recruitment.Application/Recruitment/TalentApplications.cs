using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>One row in a talent's application status timeline.</summary>
/// <param name="Id">Application id.</param>
/// <param name="RequestId">The role's request id.</param>
/// <param name="RoleTitle">The role title.</param>
/// <param name="City">The role city.</param>
/// <param name="Status">Pipeline status (applied/reviewed/shortlisted/hired/rejected).</param>
/// <param name="AppliedAt">When applied (UTC).</param>
/// <param name="DecidedAt">When decided (UTC), if any.</param>
public sealed record TalentApplicationDto(
    Guid Id, Guid RequestId, string RoleTitle, string City, string Status, DateTimeOffset AppliedAt, DateTimeOffset? DecidedAt);

/// <summary>Lists a talent's applications with role details, for their status timeline.</summary>
/// <param name="TalentId">The talent id.</param>
/// <param name="Limit">Maximum rows (1–100).</param>
public sealed record GetTalentApplicationsQuery(Guid TalentId, int Limit = 50) : IQuery<IReadOnlyList<TalentApplicationDto>>;

/// <summary>Handles <see cref="GetTalentApplicationsQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetTalentApplicationsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetTalentApplicationsQuery, IReadOnlyList<TalentApplicationDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TalentApplicationDto>>> HandleAsync(GetTalentApplicationsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var limit = Math.Clamp(query.Limit, 1, 100);
        var applications = await _repository.ListApplicationsForTalentAsync(query.TalentId, 0, limit, cancellationToken).ConfigureAwait(false);

        var rows = new List<TalentApplicationDto>(applications.Count);
        var requestCache = new Dictionary<Guid, RecruitmentRequest?>();
        foreach (var application in applications)
        {
            var requestId = application.RequestId.Value;
            if (!requestCache.TryGetValue(requestId, out var request))
            {
                request = await _repository.GetByIdAsync(application.RequestId, cancellationToken).ConfigureAwait(false);
                requestCache[requestId] = request;
            }

            rows.Add(new TalentApplicationDto(
                application.Id.Value,
                requestId,
                request?.Title ?? "Role",
                request?.City ?? string.Empty,
                application.Status,
                application.AppliedAt,
                application.DecidedAt));
        }

        return rows;
    }
}
