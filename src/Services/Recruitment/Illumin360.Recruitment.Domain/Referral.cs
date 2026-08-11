using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// An employee/network referral of a candidate for a requisition. Service-owned + migration-managed,
/// keyed by the (externally-seeded) request id. Referrals are a sourcing channel distinct from a direct
/// application.
/// </summary>
public sealed class Referral : Entity<Guid>
{
    private Referral(Guid id)
        : base(id)
    {
    }

    /// <summary>The requisition the candidate is referred for.</summary>
    public Guid RequestId { get; private init; }

    /// <summary>The referrer's name (the employee making the referral).</summary>
    public string ReferrerName { get; private set; } = string.Empty;

    /// <summary>The referrer's email, if any (lower-cased).</summary>
    public string? ReferrerEmail { get; private set; }

    /// <summary>The referred candidate's name.</summary>
    public string CandidateName { get; private set; } = string.Empty;

    /// <summary>The referred candidate's email (lower-cased).</summary>
    public string CandidateEmail { get; private set; } = string.Empty;

    /// <summary>An optional note ("worked with them at…").</summary>
    public string? Note { get; private set; }

    /// <summary>When the referral was submitted (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Records a referral.</summary>
    /// <param name="requestId">The requisition (required).</param>
    /// <param name="referrerName">The referrer's name (required).</param>
    /// <param name="referrerEmail">The referrer's email (optional; validated when present).</param>
    /// <param name="candidateName">The candidate's name (required).</param>
    /// <param name="candidateEmail">The candidate's email (required, must look like an address).</param>
    /// <param name="note">Optional note (≤ 1000 chars).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The referral, or a validation error.</returns>
    public static Result<Referral> Create(
        Guid requestId,
        string referrerName,
        string? referrerEmail,
        string candidateName,
        string candidateEmail,
        string? note,
        DateTimeOffset createdAt)
    {
        if (requestId == Guid.Empty)
        {
            return Error.Validation("referral.request_required", "A requisition id is required.");
        }

        if (string.IsNullOrWhiteSpace(referrerName))
        {
            return Error.Validation("referral.referrer_required", "A referrer name is required.");
        }

        if (string.IsNullOrWhiteSpace(candidateName))
        {
            return Error.Validation("referral.candidate_required", "A candidate name is required.");
        }

        if (!LooksLikeEmail(candidateEmail))
        {
            return Error.Validation("referral.candidate_email_invalid", "A valid candidate email is required.");
        }

        if (!string.IsNullOrWhiteSpace(referrerEmail) && !LooksLikeEmail(referrerEmail))
        {
            return Error.Validation("referral.referrer_email_invalid", "The referrer email is not valid.");
        }

        if (note is { Length: > 1000 })
        {
            return Error.Validation("referral.note_too_long", "A note must be 1000 characters or fewer.");
        }

        return new Referral(Guid.NewGuid())
        {
            RequestId = requestId,
            ReferrerName = referrerName.Trim(),
            ReferrerEmail = string.IsNullOrWhiteSpace(referrerEmail) ? null : referrerEmail.Trim().ToLowerInvariant(),
            CandidateName = candidateName.Trim(),
            CandidateEmail = candidateEmail.Trim().ToLowerInvariant(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt = createdAt,
        };
    }

    private static bool LooksLikeEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        var at = trimmed.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at < trimmed.Length - 1;
    }
}
