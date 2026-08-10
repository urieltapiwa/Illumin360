namespace Illumin360.Recruitment.Domain;

/// <summary>Strongly-typed identifier for an <see cref="Offer"/>.</summary>
/// <param name="Value">The underlying GUID value.</param>
public readonly record struct OfferId(Guid Value)
{
    /// <summary>Creates a new, unique offer identifier.</summary>
    public static OfferId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
