namespace Illumin360.Employers.Domain;

/// <summary>Strongly-typed identity for an <see cref="Employer"/>.</summary>
/// <param name="Value">The underlying GUID.</param>
public readonly record struct EmployerId(Guid Value)
{
    /// <summary>Creates a new random identity.</summary>
    public static EmployerId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
