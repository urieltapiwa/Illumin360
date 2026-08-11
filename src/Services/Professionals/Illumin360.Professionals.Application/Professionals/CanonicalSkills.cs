using Illumin360.Matching;
using Illumin360.Professionals.Application.Abstractions;
using Illumin360.SharedKernel;

namespace Illumin360.Professionals.Application.Professionals;

/// <summary>One of the professional's skills mapped onto the canonical taxonomy.</summary>
/// <param name="Id">The skill's id (for editing).</param>
/// <param name="Raw">The raw skill name as entered.</param>
/// <param name="CanonicalId">The canonical skill id.</param>
/// <param name="CanonicalDisplay">The canonical display name.</param>
/// <param name="Aliased">Whether the raw name differs from its canonical form (a synonym/typo tidy-up).</param>
public sealed record CanonicalSkillDto(Guid Id, string Raw, string CanonicalId, string CanonicalDisplay, bool Aliased);

/// <summary>A group of the professional's skills that collapse to the same canonical skill.</summary>
/// <param name="CanonicalDisplay">The canonical skill they share.</param>
/// <param name="Members">The raw skill names that map to it (candidates to merge).</param>
public sealed record SkillDuplicateDto(string CanonicalDisplay, IReadOnlyList<string> Members);

/// <summary>The professional's skills normalised onto the taxonomy, plus any duplicate groups to merge.</summary>
/// <param name="Skills">Each skill mapped to its canonical form.</param>
/// <param name="Duplicates">Groups of skills that mean the same canonical skill.</param>
public sealed record CanonicalSkillsDto(IReadOnlyList<CanonicalSkillDto> Skills, IReadOnlyList<SkillDuplicateDto> Duplicates);

/// <summary>Maps the current ("me") professional's skills onto the canonical taxonomy + finds duplicates.</summary>
public sealed record GetCanonicalSkillsQuery : IQuery<CanonicalSkillsDto>;

/// <summary>Handles <see cref="GetCanonicalSkillsQuery"/> using the shared <see cref="SkillTaxonomy"/>.</summary>
/// <param name="repository">The professional repository.</param>
public sealed class GetCanonicalSkillsQueryHandler(IProfessionalRepository repository)
    : IQueryHandler<GetCanonicalSkillsQuery, CanonicalSkillsDto>
{
    private readonly IProfessionalRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<CanonicalSkillsDto>> HandleAsync(GetCanonicalSkillsQuery query, CancellationToken cancellationToken)
    {
        var meId = await _repository.GetDefaultProfessionalIdAsync(cancellationToken).ConfigureAwait(false);
        if (meId is not { } id)
        {
            return new CanonicalSkillsDto([], []);
        }

        var skills = await _repository.ListSkillsAsync(id, cancellationToken).ConfigureAwait(false);

        var mapped = skills.Select(s =>
        {
            var canonical = SkillTaxonomy.Canonicalize(s.Name);
            var aliased = !string.Equals(s.Name.Trim(), canonical.Display, StringComparison.OrdinalIgnoreCase);
            return new CanonicalSkillDto(s.Id, s.Name, canonical.Id, canonical.Display, aliased);
        }).ToList();

        var duplicates = SkillTaxonomy.DuplicateGroups(skills.Select(s => s.Name))
            .Select(g => new SkillDuplicateDto(g.Canonical.Display, g.Members))
            .ToList();

        return new CanonicalSkillsDto(mapped, duplicates);
    }
}
