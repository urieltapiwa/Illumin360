using Illumin360.SharedKernel;
using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Domain;

namespace Illumin360.Professionals.Application.Professionals;

/// <summary>
/// Query for a professional's dashboard. When <see cref="Id"/> is <see langword="null"/> the default
/// (demo) professional is returned — the portal's "me" view before real per-user identity is wired.
/// </summary>
/// <param name="Id">The professional id, or <see langword="null"/> for the default professional.</param>
public sealed record GetProfessionalDashboardQuery(Guid? Id = null) : IQuery<ProfessionalDashboardDto>;

/// <summary>Handles <see cref="GetProfessionalDashboardQuery"/>.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class GetProfessionalDashboardQueryHandler(IProfessionalRepository repository)
    : IQueryHandler<GetProfessionalDashboardQuery, ProfessionalDashboardDto>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ProfessionalDashboardDto>> HandleAsync(
        GetProfessionalDashboardQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var dashboard = query.Id is { } id
            ? await _repository.GetDashboardAsync(new ProfessionalId(id), cancellationToken).ConfigureAwait(false)
            : await _repository.GetDefaultDashboardAsync(cancellationToken).ConfigureAwait(false);

        if (dashboard is null)
        {
            return Error.NotFound("professional.not_found", "No matching professional was found.");
        }

        return ProfessionalDashboardDto.FromDomain(dashboard);
    }
}
