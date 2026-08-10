namespace Illumin360.Recruitment.Domain;

/// <summary>Strongly-typed identifier for an <see cref="Interview"/>.</summary>
/// <param name="Value">The underlying GUID value.</param>
public readonly record struct InterviewId(Guid Value)
{
    /// <summary>Creates a new, unique interview identifier.</summary>
    public static InterviewId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
