using Illumin360.SharedKernel;

namespace Illumin360.Professionals.Domain;

/// <summary>
/// A peer/recruiter endorsement (or reference) of a specific <see cref="ProfessionalSkill"/>. Owned +
/// migration-managed by the service; unique per (skill, endorser).
/// </summary>
public sealed class SkillEndorsement : Entity<Guid>
{
    private SkillEndorsement(Guid id)
        : base(id)
    {
    }

    /// <summary>The endorsed skill.</summary>
    public Guid SkillId { get; private init; }

    /// <summary>Who gave the endorsement (name / organisation).</summary>
    public string Endorser { get; private set; } = string.Empty;

    /// <summary>Optional short reference note.</summary>
    public string? Note { get; private set; }

    /// <summary>When the endorsement was given (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates an endorsement, enforcing a non-empty endorser.</summary>
    /// <param name="skillId">The endorsed skill (required).</param>
    /// <param name="endorser">Endorser name (required).</param>
    /// <param name="note">Optional reference note (≤ 500 chars).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The endorsement, or a validation error.</returns>
    public static Result<SkillEndorsement> Create(Guid skillId, string endorser, string? note, DateTimeOffset createdAt)
    {
        if (skillId == Guid.Empty)
        {
            return Error.Validation("endorsement.skill_required", "A skill id is required.");
        }

        if (string.IsNullOrWhiteSpace(endorser))
        {
            return Error.Validation("endorsement.endorser_required", "An endorser name is required.");
        }

        if (note is { Length: > 500 })
        {
            return Error.Validation("endorsement.note_too_long", "A reference note must be 500 characters or fewer.");
        }

        return new SkillEndorsement(Guid.NewGuid())
        {
            SkillId = skillId,
            Endorser = endorser.Trim(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt = createdAt,
        };
    }
}
