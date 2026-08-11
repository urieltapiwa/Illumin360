using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// Talent-side match-signal points captured at apply-time (the candidate's city / role-affinity / skill
/// fit for the role, 0–100 each), supplied by the applying portal since Recruitment can't see the talent's
/// profile. Service-owned + migration-managed, keyed 1:1 by the (externally-seeded) application id; folded
/// into the labelled <see cref="MatchOutcome"/> as extra LTR features at decision time.
/// </summary>
public sealed class ApplicationFeatures : Entity<Guid>
{
    private ApplicationFeatures(Guid id)
        : base(id)
    {
    }

    /// <summary>The application these features describe.</summary>
    public Guid ApplicationId { get; private init; }

    /// <summary>City-fit signal (0–100).</summary>
    public int CitySignal { get; private set; }

    /// <summary>Role-affinity signal (0–100).</summary>
    public int RoleSignal { get; private set; }

    /// <summary>Skill-fit signal (0–100).</summary>
    public int SkillSignal { get; private set; }

    /// <summary>When captured (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Captures the talent-side signal points for an application.</summary>
    /// <param name="applicationId">The application (required).</param>
    /// <param name="citySignal">City-fit points (clamped 0–100).</param>
    /// <param name="roleSignal">Role-affinity points (clamped 0–100).</param>
    /// <param name="skillSignal">Skill-fit points (clamped 0–100).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The features, or a validation error.</returns>
    public static Result<ApplicationFeatures> Create(Guid applicationId, int citySignal, int roleSignal, int skillSignal, DateTimeOffset createdAt)
    {
        if (applicationId == Guid.Empty)
        {
            return Error.Validation("appfeatures.application_required", "An application id is required.");
        }

        return new ApplicationFeatures(Guid.NewGuid())
        {
            ApplicationId = applicationId,
            CitySignal = Math.Clamp(citySignal, 0, 100),
            RoleSignal = Math.Clamp(roleSignal, 0, 100),
            SkillSignal = Math.Clamp(skillSignal, 0, 100),
            CreatedAt = createdAt,
        };
    }
}
