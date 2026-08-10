using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// An onboarding checklist created when a candidate is hired, tracking the steps to bring them on board.
/// Owned and migration-managed by the Recruitment service. Tasks are modelled as a separate
/// <see cref="OnboardingTask"/> entity keyed by <see cref="OnboardingChecklistId"/>.
/// </summary>
public sealed class OnboardingChecklist : Entity<OnboardingChecklistId>
{
    /// <summary>The default onboarding steps seeded for a new hire, in order.</summary>
    public static readonly IReadOnlyList<string> DefaultTasks =
    [
        "Sign employment contract",
        "Submit banking & tax details",
        "Provision IT equipment & accounts",
        "Complete first-day orientation",
        "Add to payroll",
    ];

    // EF Core materialisation constructor.
    private OnboardingChecklist(OnboardingChecklistId id)
        : base(id)
    {
    }

    /// <summary>The application this checklist is for.</summary>
    public Guid ApplicationId { get; private init; }

    /// <summary>Role title captured on the checklist.</summary>
    public string RoleTitle { get; private set; } = string.Empty;

    /// <summary>When the checklist was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Starts an onboarding checklist for a hired application.</summary>
    /// <param name="applicationId">The application (required).</param>
    /// <param name="roleTitle">Role title (required).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The checklist, or a validation error.</returns>
    public static Result<OnboardingChecklist> Start(Guid applicationId, string roleTitle, DateTimeOffset createdAt)
    {
        if (applicationId == Guid.Empty)
        {
            return Error.Validation("onboarding.application_required", "An application id is required.");
        }

        if (string.IsNullOrWhiteSpace(roleTitle))
        {
            return Error.Validation("onboarding.role_required", "A role title is required.");
        }

        return new OnboardingChecklist(OnboardingChecklistId.New())
        {
            ApplicationId = applicationId,
            RoleTitle = roleTitle.Trim(),
            CreatedAt = createdAt,
        };
    }

    /// <summary>Rehydrates a fully-specified checklist for demo seeding / import.</summary>
    /// <param name="id">Identity.</param>
    /// <param name="applicationId">Application id.</param>
    /// <param name="roleTitle">Role title.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The hydrated checklist.</returns>
    public static OnboardingChecklist Seed(Guid id, Guid applicationId, string roleTitle, DateTimeOffset createdAt)
        => new(new OnboardingChecklistId(id))
        {
            ApplicationId = applicationId,
            RoleTitle = roleTitle,
            CreatedAt = createdAt,
        };
}
