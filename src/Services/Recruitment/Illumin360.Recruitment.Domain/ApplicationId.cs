namespace Illumin360.Recruitment.Domain;

/// <summary>Strongly-typed identifier for a <see cref="RecruitmentApplication"/>.</summary>
/// <param name="Value">The underlying GUID value.</param>
public readonly record struct ApplicationId(Guid Value)
{
    /// <summary>Creates a new, unique application identifier.</summary>
    public static ApplicationId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
