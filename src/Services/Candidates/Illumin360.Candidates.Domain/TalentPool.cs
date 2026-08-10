using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Domain;

/// <summary>Strongly-typed identifier for a <see cref="TalentPool"/>.</summary>
/// <param name="Value">The underlying GUID value.</param>
public readonly record struct TalentPoolId(Guid Value)
{
    /// <summary>Creates a new, unique talent-pool identifier.</summary>
    public static TalentPoolId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>A named recruiter shortlist / talent pool. Owned + migration-managed by the service.</summary>
public sealed class TalentPool : Entity<TalentPoolId>
{
    private TalentPool(TalentPoolId id) : base(id) { }

    /// <summary>Pool name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>When the pool was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a pool, enforcing a non-empty name.</summary>
    /// <param name="name">Pool name (required).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The pool, or a validation error.</returns>
    public static Result<TalentPool> Create(string name, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("pool.name_required", "A pool name is required.");
        }

        return new TalentPool(TalentPoolId.New()) { Name = name.Trim(), CreatedAt = createdAt };
    }
}

/// <summary>Membership of a candidate in a talent pool.</summary>
public sealed class TalentPoolMember : Entity<Guid>
{
    private TalentPoolMember(Guid id) : base(id) { }

    /// <summary>Creates a membership.</summary>
    /// <param name="id">Row identity.</param>
    /// <param name="poolId">Owning pool.</param>
    /// <param name="candidateId">Candidate.</param>
    /// <param name="addedAt">When added (UTC).</param>
    public TalentPoolMember(Guid id, TalentPoolId poolId, CandidateId candidateId, DateTimeOffset addedAt)
        : base(id)
    {
        PoolId = poolId;
        CandidateId = candidateId;
        AddedAt = addedAt;
    }

    /// <summary>Owning pool.</summary>
    public TalentPoolId PoolId { get; private set; }

    /// <summary>The candidate in the pool.</summary>
    public CandidateId CandidateId { get; private set; }

    /// <summary>When the candidate was added (UTC).</summary>
    public DateTimeOffset AddedAt { get; private set; }
}
