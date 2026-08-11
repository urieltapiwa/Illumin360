using Illumin360.Matching;
using Illumin360.Professionals.Application.Abstractions;
using Illumin360.SharedKernel;

namespace Illumin360.Professionals.Application.Professionals;

/// <summary>A skill-gap analysis of the current professional against a role's required skills.</summary>
/// <param name="Matched">Required skills the professional already has.</param>
/// <param name="Missing">Required skills to learn.</param>
/// <param name="Extra">The professional's skills the role didn't ask for.</param>
/// <param name="CoveragePercent">Share of required skills covered (0–100).</param>
public sealed record SkillGapDto(IReadOnlyList<string> Matched, IReadOnlyList<string> Missing, IReadOnlyList<string> Extra, int CoveragePercent);

/// <summary>Analyses the current ("me") professional's skills against a set of required skills.</summary>
/// <param name="RequiredSkills">The role's required skills.</param>
public sealed record GetSkillGapQuery(IReadOnlyList<string> RequiredSkills) : IQuery<SkillGapDto>;

/// <summary>Handles <see cref="GetSkillGapQuery"/> using the shared <see cref="SkillGapAnalyzer"/>.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class GetSkillGapQueryHandler(IProfessionalRepository repository)
    : IQueryHandler<GetSkillGapQuery, SkillGapDto>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<SkillGapDto>> HandleAsync(GetSkillGapQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var meId = await _repository.GetDefaultProfessionalIdAsync(cancellationToken).ConfigureAwait(false);
        var mySkills = meId is { } id
            ? (await _repository.ListSkillsAsync(id, cancellationToken).ConfigureAwait(false)).Select(s => s.Name)
            : [];

        // Taxonomy-aware so synonyms match (e.g. a profile listing "JS" covers a role wanting "JavaScript").
        var gap = SkillGapAnalyzer.Analyze(mySkills, query.RequiredSkills, useTaxonomy: true);
        return new SkillGapDto(gap.Matched, gap.Missing, gap.Extra, gap.CoveragePercent);
    }
}
