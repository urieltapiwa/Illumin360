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

/// <summary>One signal's contribution to a role match ("why this match").</summary>
/// <param name="Name">Signal name.</param>
/// <param name="Points">Approximate points contributed (0–100 total).</param>
/// <param name="Reason">Human-readable explanation.</param>
public sealed record MatchSignalDto(string Name, int Points, string Reason);

/// <summary>A role match with its per-signal breakdown.</summary>
/// <param name="Score">Overall 0–100 score.</param>
/// <param name="Signals">The contributing signals.</param>
public sealed record RoleExplanationDto(int Score, IReadOnlyList<MatchSignalDto> Signals);

/// <summary>Explains how a single marketplace role scores against the current ("me") professional.</summary>
/// <param name="Role">The role to explain.</param>
public sealed record ExplainRoleQuery(RoleToScore Role) : IQuery<RoleExplanationDto>;

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

        // Derive the talent's seniority from their headline/role so it factors into the score; the role's
        // seniority comes from its title. Both are best-effort text parsing (no new profile fields).
        var talent = new TalentProfile(p.City, p.Role, [.. dashboard.Skills.Select(s => s.Name)], SalaryExpectation: null, Seniority: p.Role);

        return query.Roles
            .Select(r => new RoleScoreDto(r.Id, MatchScorer.Score(talent, new RoleListing(r.Title, r.City, r.Industry ?? string.Empty, SalaryMin: null, SalaryMax: null, Seniority: r.Title))))
            .ToList();
    }
}

/// <summary>Handles <see cref="ExplainRoleQuery"/> using the shared engine's per-signal breakdown.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class ExplainRoleQueryHandler(IProfessionalRepository repository)
    : IQueryHandler<ExplainRoleQuery, RoleExplanationDto>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<RoleExplanationDto>> HandleAsync(ExplainRoleQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Role is null)
        {
            return Error.Validation("role.required", "A role is required.");
        }

        var dashboard = await _repository.GetDefaultDashboardAsync(cancellationToken).ConfigureAwait(false);
        if (dashboard is null)
        {
            return new RoleExplanationDto(0, []);
        }

        var p = dashboard.Professional;
        var talent = new TalentProfile(p.City, p.Role, [.. dashboard.Skills.Select(s => s.Name)], SalaryExpectation: null, Seniority: p.Role);
        var role = new RoleListing(query.Role.Title, query.Role.City, query.Role.Industry ?? string.Empty, SalaryMin: null, SalaryMax: null, Seniority: query.Role.Title);

        var explanation = MatchScorer.Explain(talent, role);
        return new RoleExplanationDto(
            explanation.Score,
            explanation.Signals.Select(s => new MatchSignalDto(s.Name, s.Points, s.Reason)).ToList());
    }
}
