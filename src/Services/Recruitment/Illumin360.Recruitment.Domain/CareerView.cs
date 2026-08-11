using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// A per-role view counter for the public careers site. Service-owned + migration-managed, keyed 1:1 by
/// the (externally-seeded) request id. Incremented each time a role's careers detail page is served.
/// </summary>
public sealed class CareerView : Entity<Guid>
{
    private CareerView(Guid id)
        : base(id)
    {
    }

    /// <summary>The requisition being viewed.</summary>
    public Guid RequestId { get; private init; }

    /// <summary>Total detail-page views.</summary>
    public long Views { get; private set; }

    /// <summary>When the role was last viewed (UTC).</summary>
    public DateTimeOffset LastViewedAt { get; private set; }

    /// <summary>Creates a fresh counter for a role at its first view.</summary>
    /// <param name="requestId">The requisition (required).</param>
    /// <param name="viewedAt">The view timestamp (UTC).</param>
    /// <returns>A counter with one view recorded.</returns>
    public static CareerView First(Guid requestId, DateTimeOffset viewedAt)
        => new(Guid.NewGuid()) { RequestId = requestId, Views = 1, LastViewedAt = viewedAt };

    /// <summary>Records another view.</summary>
    /// <param name="viewedAt">The view timestamp (UTC).</param>
    public void Record(DateTimeOffset viewedAt)
    {
        Views++;
        LastViewedAt = viewedAt;
    }
}
