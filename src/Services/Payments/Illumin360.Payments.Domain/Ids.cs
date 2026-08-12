namespace Illumin360.Payments.Domain;

/// <summary>Strongly-typed identity for a <see cref="Contract"/>.</summary>
/// <param name="Value">The underlying GUID.</param>
public readonly record struct ContractId(Guid Value)
{
    /// <summary>Creates a new random identity.</summary>
    public static ContractId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Strongly-typed identity for a <see cref="Milestone"/>.</summary>
/// <param name="Value">The underlying GUID.</param>
public readonly record struct MilestoneId(Guid Value)
{
    /// <summary>Creates a new random identity.</summary>
    public static MilestoneId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
