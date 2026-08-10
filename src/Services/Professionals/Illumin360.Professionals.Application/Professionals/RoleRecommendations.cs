using Illumin360.Matching;
using Illumin360.Professionals.Application.Abstractions;
using Illumin360.SharedKernel;

namespace Illumin360.Professionals.Application.Professionals;

/// <summary>A marketplace role to score against the current professional.</summary>
/// <param name="Id">Role id (echoed back with the score).</param>
/// <param name="Title">Role title.</param>
/// <param name="City">Role city.</param>
/// <param name="Industry">Company industry (optional).</param>
public sealed record RoleToScore(Guid Id, string Title, string City, string? Industry);

/// <summary>A computed match score for a role.</summary>
/// <param name="Id">Role id.</param>
/// <param name="Score">Match score (0–100).</param>
public sealed record RoleScoreDto(Guid Id, int Score);

/// <summary>Scores a set of marketplace roles against the current ("me") professional.</summary>
/// <param name="Roles">Roles to score.</param>
public sealed record ScoreRolesQuery(IReadOnlyList<RoleToScore> Roles) : IQuery<IReadOnlyList<RoleScoreDto>>;

/// <summary>Handles <see cref="ScoreRolesQuery"/> using the shared match engine.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class ScoreRolesQueryHandler(IProfessionalRepository repository)
    : IQueryHandler<ScoreRolesQuery, IReadOnlyList<RoleScoreDto>>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RoleScoreDto>>> HandleAsync(ScoreRolesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Roles is null || query.Roles.Count == 0)
        {
            return Array.Empty<RoleScoreDto>();
        }

        var dashboard = await _repository.GetDefaultDashboardAsync(cancellationToken).ConfigureAwait(false);
        if (dashboard is null)
        {
            return Array.Empty<RoleScoreDto>();
        }

        var p = dashboard.Professional;
        var talent = new TalentProfile(p.City, p.Role, [.. dashboard.Skills.Select(s => s.Name)]);

        return query.Roles
            .Select(r => new RoleScoreDto(r.Id, MatchScorer.Score(talent, new RoleListing(r.Title, r.City, r.Industry ?? string.Empty))))
            .ToList();
    }
}
