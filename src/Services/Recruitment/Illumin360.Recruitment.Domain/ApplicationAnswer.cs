using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// A candidate's answer to an application-form question, captured at apply-time and keyed by application.
/// The question label is snapshotted so recruiter views need no join and answers survive question edits/
/// deletion. Service-owned + migration-managed.
/// </summary>
public sealed class ApplicationAnswer : Entity<Guid>
{
    private ApplicationAnswer(Guid id)
        : base(id)
    {
    }

    /// <summary>The application this answer belongs to.</summary>
    public Guid ApplicationId { get; private init; }

    /// <summary>The question that was answered.</summary>
    public Guid QuestionId { get; private init; }

    /// <summary>Snapshot of the question label at submit time.</summary>
    public string QuestionLabel { get; private set; } = string.Empty;

    /// <summary>The answer value (free text / number-as-text / "true"/"false" / chosen option).</summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>When the answer was captured (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Records a candidate's answer.</summary>
    /// <param name="applicationId">The application (required).</param>
    /// <param name="questionId">The question (required).</param>
    /// <param name="questionLabel">Snapshot of the question label (required).</param>
    /// <param name="value">The answer value (≤ 4000 chars).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The answer, or a validation error.</returns>
    public static Result<ApplicationAnswer> Create(
        Guid applicationId,
        Guid questionId,
        string questionLabel,
        string? value,
        DateTimeOffset createdAt)
    {
        if (applicationId == Guid.Empty)
        {
            return Error.Validation("answer.application_required", "An application id is required.");
        }

        if (questionId == Guid.Empty)
        {
            return Error.Validation("answer.question_required", "A question id is required.");
        }

        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length > 4000)
        {
            return Error.Validation("answer.value_too_long", "An answer must be 4000 characters or fewer.");
        }

        return new ApplicationAnswer(Guid.NewGuid())
        {
            ApplicationId = applicationId,
            QuestionId = questionId,
            QuestionLabel = string.IsNullOrWhiteSpace(questionLabel) ? "(question)" : questionLabel.Trim(),
            Value = trimmed,
            CreatedAt = createdAt,
        };
    }
}
