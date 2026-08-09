namespace Illumin360.Admin.Domain;

/// <summary>Strongly-typed identity for a <see cref="Verification"/>.</summary>
/// <param name="Value">The underlying GUID.</param>
public readonly record struct VerificationId(Guid Value)
{
    /// <summary>Creates a new random identity.</summary>
    /// <returns>A fresh <see cref="VerificationId"/>.</returns>
    public static VerificationId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
