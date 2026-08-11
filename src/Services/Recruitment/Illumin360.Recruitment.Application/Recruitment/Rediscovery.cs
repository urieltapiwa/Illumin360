using Illumin360.Matching;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A past not-hired application, projected for rediscovery scoring (all fields Recruitment-owned).</summary>
/// <param name="TalentId">The applicant's talent id.</param>
/// <param name="TalentType">Talent type (professional/student).</param>
/// <param name="PriorRequestId">The prior requisition the talent applied to.</param>
/// <param name="PriorTitle">That requisition's title.</param>
/// <param name="PriorCity">That requisition's city.</param>
/// <param name="PriorStatus">The furthest pipeline status the prior application reached.</param>
/// <param name="PriorMatchScore">The match score recorded on the prior application.</param>
/// <param name="InterviewCount">Interviews the prior application reached (0 if none/unknown).</param>
/// <param name="HadOffer">Whether the prior application received an offer.</param>
public sealed record RediscoveryPoolRow(
    Guid TalentId,
    string TalentType,
    Guid PriorRequestId,
    string PriorTitle,
    string PriorCity,
    string PriorStatus,
    decimal PriorMatchScore,
    int InterviewCount,
    bool HadOffer);

/// <summary>A rediscovered "silver-medalist" candidate for a target requisition.</summary>
/// <param name="TalentId">The candidate's talent id.</param>
/// <param name="TalentType">Talent type.</param>
/// <param name="Score">Rediscovery fit score (0–100) for the target role.</param>
/// <param name="Reason">Why they surfaced.</param>
/// <param name="PriorTitle">The role they previously applied to.</param>
/// <param name="PriorStatus">How far that prior application got.</param>
/// <param name="PriorMatchScore">Their match score on that prior role.</param>
/// <param name="InterviewCount">Interviews they reached previously.</param>
/// <param name="HadOffer">Whether they previously reached an offer.</param>
public sealed record RediscoveredCandidateDto(
    Guid TalentId,
    string TalentType,
    int Score,
    string Reason,
    string PriorTitle,
    string PriorStatus,
    decimal PriorMatchScore,
    int InterviewCount,
    bool HadOffer);

/// <summary>Rediscovers past not-hired applicants who fit a target requisition ("silver medalists").</summary>
/// <param name="RequestId">The target requisition id.</param>
/// <param name="Take">Max candidates to return.</param>
public sealed record GetRediscoveryQuery(Guid RequestId, int Take) : IQuery<IReadOnlyList<RediscoveredCandidateDto>>;

/// <summary>Handles <see cref="GetRediscoveryQuery"/> — re-ranks the past-applicant pool against a target role.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetRediscoveryQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetRediscoveryQuery, IReadOnlyList<RediscoveredCandidateDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RediscoveredCandidateDto>>> HandleAsync(GetRediscoveryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var target = await _repository.GetByIdAsync(new RequestId(query.RequestId), cancellationToken).ConfigureAwait(false);
        if (target is null)
        {
            return Error.NotFound("recruitment.request_not_found", "Requisition not found.");
        }

        // Exclude anyone already in this requisition's pipeline — rediscovery is about *past* applicants elsewhere.
        var current = await _repository.ListApplicationsAsync(new RequestId(query.RequestId), 0, 500, cancellationToken).ConfigureAwait(false);
        var currentTalentIds = current.Select(a => a.TalentId).ToHashSet();

        var pool = await _repository.ListRediscoveryPoolAsync(new RequestId(query.RequestId), cancellationToken).ConfigureAwait(false);

        var scored = pool
            .Where(r => !currentTalentIds.Contains(r.TalentId))
            .Select(r =>
            {
                var s = RediscoveryScorer.Evaluate(target.Title, target.City, r.PriorTitle, r.PriorCity, r.PriorMatchScore, r.InterviewCount, r.HadOffer);
                return new RediscoveredCandidateDto(r.TalentId, r.TalentType, s.Value, s.Reason, r.PriorTitle, r.PriorStatus, r.PriorMatchScore, r.InterviewCount, r.HadOffer);
            })

            // A talent may have several past applications — keep their strongest rediscovery match.
            .GroupBy(c => c.TalentId)
            .Select(g => g.OrderByDescending(c => c.Score).First())
            .OrderByDescending(c => c.Score)
            .ThenByDescending(c => c.PriorMatchScore)
            .Take(Math.Clamp(query.Take, 1, 100))
            .ToList();

        return scored;
    }
}
