using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// A reusable interview kit — a named question bank a recruiter runs during interviews. Each question can
/// name the skill it assesses, mapping onto the per-round <see cref="InterviewSkillRating"/> scoring.
/// Owned + migration-managed by the Recruitment service.
/// </summary>
public sealed class InterviewKit : Entity<Guid>
{
    private InterviewKit(Guid id)
        : base(id)
    {
    }

    /// <summary>Kit name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>When the kit was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a kit.</summary>
    /// <param name="name">Kit name (required, ≤ 160 chars).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The kit, or a validation error.</returns>
    public static Result<InterviewKit> Create(string name, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
        {
            return Error.Validation("kit.name_invalid", "A kit name (≤ 160 chars) is required.");
        }

        return new InterviewKit(Guid.NewGuid())
        {
            Name = name.Trim(),
            CreatedAt = createdAt,
        };
    }
}

/// <summary>One question in an <see cref="InterviewKit"/>, optionally assessing a named skill.</summary>
public sealed class InterviewKitQuestion : Entity<Guid>
{
    private InterviewKitQuestion(Guid id)
        : base(id)
    {
    }

    /// <summary>Owning kit.</summary>
    public Guid KitId { get; private set; }

    /// <summary>Order within the kit.</summary>
    public int QuestionOrder { get; private set; }

    /// <summary>The question text.</summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>The skill this question assesses (optional).</summary>
    public string? Skill { get; private set; }

    /// <summary>When the question was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Adds a question to a kit.</summary>
    /// <param name="kitId">Owning kit id.</param>
    /// <param name="questionOrder">Order within the kit.</param>
    /// <param name="text">Question text (required, ≤ 500 chars).</param>
    /// <param name="skill">Skill assessed (optional, ≤ 80 chars).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The question, or a validation error.</returns>
    public static Result<InterviewKitQuestion> Create(Guid kitId, int questionOrder, string text, string? skill, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 500)
        {
            return Error.Validation("kit.question_invalid", "Question text (≤ 500 chars) is required.");
        }

        if (skill is { Length: > 80 })
        {
            return Error.Validation("kit.skill_too_long", "The skill label is too long.");
        }

        return new InterviewKitQuestion(Guid.NewGuid())
        {
            KitId = kitId,
            QuestionOrder = questionOrder,
            Text = text.Trim(),
            Skill = string.IsNullOrWhiteSpace(skill) ? null : skill.Trim(),
            CreatedAt = createdAt,
        };
    }
}
