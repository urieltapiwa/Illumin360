using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Domain;

/// <summary>A private recruiter note attached to a candidate. Owned + migration-managed by the service.</summary>
public sealed class CandidateNote : Entity<Guid>
{
    private CandidateNote(Guid id)
        : base(id)
    {
    }

    /// <summary>The candidate the note is about.</summary>
    public CandidateId CandidateId { get; private init; }

    /// <summary>Who wrote the note (recruiter display name / email).</summary>
    public string Author { get; private set; } = string.Empty;

    /// <summary>The note body.</summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>When the note was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a recruiter note, enforcing a non-empty body.</summary>
    /// <param name="candidateId">The candidate (required).</param>
    /// <param name="author">Author display name (defaults to "Recruiter" when blank).</param>
    /// <param name="body">Note body (required, ≤ 2000 chars).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The note, or a validation error.</returns>
    public static Result<CandidateNote> Create(CandidateId candidateId, string? author, string body, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return Error.Validation("note.body_required", "A note body is required.");
        }

        if (body.Length > 2000)
        {
            return Error.Validation("note.body_too_long", "A note must be 2000 characters or fewer.");
        }

        return new CandidateNote(Guid.NewGuid())
        {
            CandidateId = candidateId,
            Author = string.IsNullOrWhiteSpace(author) ? "Recruiter" : author.Trim(),
            Body = body.Trim(),
            CreatedAt = createdAt,
        };
    }
}

/// <summary>A tag / label applied to a candidate (unique per candidate, case-insensitive).</summary>
public sealed class CandidateTag : Entity<Guid>
{
    private CandidateTag(Guid id)
        : base(id)
    {
    }

    /// <summary>The tagged candidate.</summary>
    public CandidateId CandidateId { get; private init; }

    /// <summary>The tag label (stored lower-cased for uniqueness).</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>When the tag was applied (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a tag, normalising the label to a trimmed, lower-cased slug.</summary>
    /// <param name="candidateId">The candidate (required).</param>
    /// <param name="label">The tag label (required, ≤ 40 chars).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The tag, or a validation error.</returns>
    public static Result<CandidateTag> Create(CandidateId candidateId, string label, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return Error.Validation("tag.label_required", "A tag label is required.");
        }

        var normalized = label.Trim().ToLowerInvariant();
        if (normalized.Length > 40)
        {
            return Error.Validation("tag.label_too_long", "A tag must be 40 characters or fewer.");
        }

        return new CandidateTag(Guid.NewGuid())
        {
            CandidateId = candidateId,
            Label = normalized,
            CreatedAt = createdAt,
        };
    }
}
