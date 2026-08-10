using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Matching;
using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>A candidate ranked against a role by the shared match engine.</summary>
/// <param name="Id">Candidate id.</param>
/// <param name="Name">Full name.</param>
/// <param name="City">Home city.</param>
/// <param name="Headline">Public headline, if any.</param>
/// <param name="Score">Match score (0–100).</param>
public sealed record RankedCandidateDto(Guid Id, string Name, string City, string? Headline, int Score);

/// <summary>Ranks candidates against a role (employer "top candidates for this role").</summary>
/// <param name="Title">Role title (required).</param>
/// <param name="City">Role city (optional).</param>
/// <param name="Limit">Maximum candidates to return (1–50).</param>
public sealed record GetTopCandidatesQuery(string Title, string? City, int Limit = 10) : IQuery<IReadOnlyList<RankedCandidateDto>>;

/// <summary>Handles <see cref="GetTopCandidatesQuery"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class GetTopCandidatesQueryHandler(ICandidateRepository repository)
    : IQueryHandler<GetTopCandidatesQuery, IReadOnlyList<RankedCandidateDto>>
{
    // Scoring pool cap — score up to this many candidates, then return the top N.
    private const int PoolSize = 200;

    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RankedCandidateDto>>> HandleAsync(GetTopCandidatesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.Title))
        {
            return Error.Validation("candidates.title_required", "A role title is required to rank candidates.");
        }

        var limit = Math.Clamp(query.Limit, 1, 50);
        var listing = new RoleListing(query.Title, query.City ?? string.Empty, string.Empty);

        var candidates = await _repository.ListAsync(null, 0, PoolSize, cancellationToken).ConfigureAwait(false);

        return candidates
            .Select(c => new RankedCandidateDto(
                c.Id.Value,
                $"{c.FirstName} {c.LastName}".Trim(),
                c.City,
                c.PublicHeadline,
                MatchScorer.Score(new TalentProfile(c.City, c.PublicHeadline ?? string.Empty, []), listing)))
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .ToList();
    }
}
