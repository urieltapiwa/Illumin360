namespace Illumin360.Recruitment.Domain;

/// <summary>Strongly-typed identifier for a <see cref="RecruitmentRequest"/>.</summary>
/// <param name="Value">The underlying GUID value.</param>
public readonly record struct RequestId(Guid Value)
{
    /// <summary>Creates a new, unique request identifier.</summary>
    public static RequestId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
