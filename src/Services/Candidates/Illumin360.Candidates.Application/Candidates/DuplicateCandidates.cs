using Illumin360.Candidates.Application.Abstractions;
using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>A cluster of candidates that appear to be duplicates of one another.</summary>
/// <param name="Name">The shared normalised name.</param>
/// <param name="Count">Number of candidates in the cluster.</param>
/// <param name="Candidates">The suspected-duplicate candidate records.</param>
public sealed record DuplicateGroupDto(string Name, int Count, IReadOnlyList<CandidateDto> Candidates);

/// <summary>
/// Query: find suspected-duplicate candidates — records that share the same case-insensitive
/// first+last name. Optionally also require the same city for a tighter match.
/// </summary>
/// <param name="SameCityOnly">When true, only groups whose members also share a city are returned.</param>
public sealed record FindDuplicateCandidatesQuery(bool SameCityOnly = false) : IQuery<IReadOnlyList<DuplicateGroupDto>>;

/// <summary>Handles <see cref="FindDuplicateCandidatesQuery"/> by clustering the pool in memory.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class FindDuplicateCandidatesQueryHandler(ICandidateRepository repository)
    : IQueryHandler<FindDuplicateCandidatesQuery, IReadOnlyList<DuplicateGroupDto>>
{
    // Scan cap — the pool is small; cluster up to this many records.
    private const int ScanSize = 1000;

    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DuplicateGroupDto>>> HandleAsync(FindDuplicateCandidatesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var candidates = await _repository.ListAsync(null, 0, ScanSize, cancellationToken).ConfigureAwait(false);

        static string NameKey(string first, string last)
            => $"{first.Trim()} {last.Trim()}".ToLowerInvariant();

        var groups = candidates
            .GroupBy(c => query.SameCityOnly
                ? $"{NameKey(c.FirstName, c.LastName)}|{c.City.Trim().ToLowerInvariant()}"
                : NameKey(c.FirstName, c.LastName))
            .Where(g => g.Count() > 1)
            .Select(g =>
            {
                var members = g.OrderBy(c => c.CreatedAt).ToList();
                var display = $"{members[0].FirstName} {members[0].LastName}".Trim();
                return new DuplicateGroupDto(display, members.Count, members.Select(CandidateDto.FromDomain).ToList());
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return groups;
    }
}
