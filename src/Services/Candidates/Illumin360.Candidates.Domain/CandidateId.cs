namespace Illumin360.Candidates.Domain;

/// <summary>Strongly-typed identifier for a <see cref="Candidate"/>.</summary>
/// <param name="Value">The underlying GUID value.</param>
public readonly record struct CandidateId(Guid Value)
{
    /// <summary>Creates a new, unique candidate identifier.</summary>
    public static CandidateId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
