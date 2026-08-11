using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// A labelled hiring outcome captured when an application reaches a terminal decision (hired / rejected).
/// Records the match score the ranker produced alongside the actual outcome so the current ranker's
/// quality can be measured and, later, a learning-to-rank model can be trained on real labels. Service-
/// owned + migration-managed; one row per application (1:1).
/// </summary>
public sealed class MatchOutcome : Entity<Guid>
{
    private MatchOutcome(Guid id)
        : base(id)
    {
    }

    /// <summary>The application that was decided.</summary>
    public Guid ApplicationId { get; private init; }

    /// <summary>The requisition applied to.</summary>
    public Guid RequestId { get; private init; }

    /// <summary>The talent.</summary>
    public Guid TalentId { get; private init; }

    /// <summary>Talent type (professional/student) — a coarse source feature.</summary>
    public string TalentType { get; private set; } = string.Empty;

    /// <summary>The ranker's match score (0–100) at decision time — the feature under evaluation.</summary>
    public decimal MatchScore { get; private set; }

    /// <summary>The label: <c>hired</c> or <c>rejected</c>.</summary>
    public string Outcome { get; private set; } = string.Empty;

    /// <summary>When the decision was recorded (UTC).</summary>
    public DateTimeOffset DecidedAt { get; private set; }

    /// <summary>Whether this is a positive (hire) label.</summary>
    public bool IsHire => string.Equals(Outcome, "hired", StringComparison.OrdinalIgnoreCase);

    /// <summary>Captures the outcome for a decided application.</summary>
    /// <param name="applicationId">The application (required).</param>
    /// <param name="requestId">The requisition (required).</param>
    /// <param name="talentId">The talent (required).</param>
    /// <param name="talentType">Talent type.</param>
    /// <param name="matchScore">The ranker's score (0–100).</param>
    /// <param name="hired">True for a hire, false for a rejection.</param>
    /// <param name="decidedAt">Decision timestamp (UTC).</param>
    /// <returns>The outcome, or a validation error.</returns>
    public static Result<MatchOutcome> Capture(Guid applicationId, Guid requestId, Guid talentId, string? talentType, decimal matchScore, bool hired, DateTimeOffset decidedAt)
    {
        if (applicationId == Guid.Empty)
        {
            return Error.Validation("outcome.application_required", "An application id is required.");
        }

        return new MatchOutcome(Guid.NewGuid())
        {
            ApplicationId = applicationId,
            RequestId = requestId,
            TalentId = talentId,
            TalentType = string.IsNullOrWhiteSpace(talentType) ? "unknown" : talentType.Trim().ToLowerInvariant(),
            MatchScore = Math.Clamp(matchScore, 0, 100),
            Outcome = hired ? "hired" : "rejected",
            DecidedAt = decidedAt,
        };
    }
}
