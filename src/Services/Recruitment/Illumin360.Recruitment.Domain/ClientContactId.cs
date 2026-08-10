namespace Illumin360.Recruitment.Domain;

/// <summary>Strongly-typed identifier for a <see cref="ClientContact"/>.</summary>
/// <param name="Value">The underlying GUID value.</param>
public readonly record struct ClientContactId(Guid Value)
{
    /// <summary>Creates a new, unique contact identifier.</summary>
    public static ClientContactId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
