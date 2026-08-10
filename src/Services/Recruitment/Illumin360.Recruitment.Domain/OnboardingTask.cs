using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>A single step on an <see cref="OnboardingChecklist"/>.</summary>
public sealed class OnboardingTask : Entity<OnboardingTaskId>
{
    // EF Core materialisation constructor.
    private OnboardingTask(OnboardingTaskId id)
        : base(id)
    {
    }

    /// <summary>The owning checklist.</summary>
    public OnboardingChecklistId ChecklistId { get; private init; }

    /// <summary>The task label.</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>Presentation order.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Whether the task is complete.</summary>
    public bool IsDone { get; private set; }

    /// <summary>When the task was completed (UTC), if applicable.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Creates a task on a checklist.</summary>
    /// <param name="checklistId">The owning checklist (required).</param>
    /// <param name="label">The task label (required).</param>
    /// <param name="sortOrder">Presentation order.</param>
    /// <returns>The task, or a validation error.</returns>
    public static Result<OnboardingTask> Create(OnboardingChecklistId checklistId, string label, int sortOrder)
    {
        if (checklistId.Value == Guid.Empty)
        {
            return Error.Validation("onboarding.checklist_required", "A checklist id is required.");
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return Error.Validation("onboarding.label_required", "A task label is required.");
        }

        return new OnboardingTask(OnboardingTaskId.New())
        {
            ChecklistId = checklistId,
            Label = label.Trim(),
            SortOrder = sortOrder,
            IsDone = false,
        };
    }

    /// <summary>Rehydrates a fully-specified task for demo seeding / import.</summary>
    /// <param name="id">Identity.</param>
    /// <param name="checklistId">Owning checklist id.</param>
    /// <param name="label">Label.</param>
    /// <param name="sortOrder">Presentation order.</param>
    /// <param name="isDone">Whether complete.</param>
    /// <param name="completedAt">Completion timestamp (UTC), if any.</param>
    /// <returns>The hydrated task.</returns>
    public static OnboardingTask Seed(Guid id, Guid checklistId, string label, int sortOrder, bool isDone, DateTimeOffset? completedAt)
        => new(new OnboardingTaskId(id))
        {
            ChecklistId = new OnboardingChecklistId(checklistId),
            Label = label,
            SortOrder = sortOrder,
            IsDone = isDone,
            CompletedAt = completedAt,
        };

    /// <summary>Sets the task's completion state.</summary>
    /// <param name="done">Whether the task is complete.</param>
    /// <param name="at">Timestamp for the change (UTC).</param>
    public void SetDone(bool done, DateTimeOffset at)
    {
        IsDone = done;
        CompletedAt = done ? at : null;
    }
}
