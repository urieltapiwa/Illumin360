namespace Illumin360.Recruitment.Domain;

/// <summary>Strongly-typed identifier for an <see cref="OnboardingChecklist"/>.</summary>
/// <param name="Value">The underlying GUID value.</param>
public readonly record struct OnboardingChecklistId(Guid Value)
{
    /// <summary>Creates a new, unique checklist identifier.</summary>
    public static OnboardingChecklistId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Strongly-typed identifier for an <see cref="OnboardingTask"/>.</summary>
/// <param name="Value">The underlying GUID value.</param>
public readonly record struct OnboardingTaskId(Guid Value)
{
    /// <summary>Creates a new, unique task identifier.</summary>
    public static OnboardingTaskId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
