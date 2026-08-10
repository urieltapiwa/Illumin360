namespace Illumin360.Recruitment.Domain;

/// <summary>Strongly-typed identifier for a <see cref="SavedSearch"/>.</summary>
/// <param name="Value">The underlying GUID value.</param>
public readonly record struct SavedSearchId(Guid Value)
{
    /// <summary>Creates a new, unique saved-search identifier.</summary>
    public static SavedSearchId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
