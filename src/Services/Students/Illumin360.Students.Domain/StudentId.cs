namespace Illumin360.Students.Domain;

/// <summary>Strongly-typed identity for a <see cref="Student"/>.</summary>
/// <param name="Value">The underlying GUID.</param>
public readonly record struct StudentId(Guid Value)
{
    /// <summary>Creates a new random identity.</summary>
    /// <returns>A fresh <see cref="StudentId"/>.</returns>
    public static StudentId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
