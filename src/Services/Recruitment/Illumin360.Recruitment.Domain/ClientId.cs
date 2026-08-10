namespace Illumin360.Recruitment.Domain;

/// <summary>Strongly-typed identifier for a CRM <see cref="Client"/>.</summary>
/// <param name="Value">The underlying GUID value.</param>
public readonly record struct ClientId(Guid Value)
{
    /// <summary>Creates a new, unique client identifier.</summary>
    public static ClientId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
