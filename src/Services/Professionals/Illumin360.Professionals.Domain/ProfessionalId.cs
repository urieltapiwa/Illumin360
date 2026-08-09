namespace Illumin360.Professionals.Domain;

/// <summary>Strongly-typed identity for a <see cref="Professional"/>.</summary>
/// <param name="Value">The underlying GUID.</param>
public readonly record struct ProfessionalId(Guid Value)
{
    /// <summary>Creates a new random identity.</summary>
    /// <returns>A fresh <see cref="ProfessionalId"/>.</returns>
    public static ProfessionalId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
