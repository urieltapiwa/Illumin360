using Illumin360.SharedKernel;

namespace Illumin360.Billing.Domain;

/// <summary>Subscription lifecycle.</summary>
public enum SubscriptionStatus
{
    /// <summary>In a trial (not yet charged).</summary>
    Trialing,

    /// <summary>Active + paid for the current period.</summary>
    Active,

    /// <summary>A renewal charge failed; access at risk pending dunning.</summary>
    PastDue,

    /// <summary>Cancelled (terminal).</summary>
    Canceled,
}

/// <summary>
/// A customer's (employer/tenant's) subscription to a <see cref="Plan"/>. Tracks the current billing period and
/// the provider's recurring reference. Entitlements derive from the plan while the subscription is
/// <see cref="SubscriptionStatus.Active"/> (or Trialing/PastDue during a grace window — decided by the caller).
/// </summary>
public sealed class Subscription : Entity<Guid>
{
    private Subscription(Guid id)
        : base(id)
    {
    }

    /// <summary>The customer (employer/tenant) id.</summary>
    public Guid CustomerId { get; private set; }

    /// <summary>The subscribed plan.</summary>
    public Guid PlanId { get; private set; }

    /// <summary>Status.</summary>
    public SubscriptionStatus Status { get; private set; }

    /// <summary>Current billing period start (UTC).</summary>
    public DateTimeOffset CurrentPeriodStart { get; private set; }

    /// <summary>Current billing period end / next-charge date (UTC).</summary>
    public DateTimeOffset CurrentPeriodEnd { get; private set; }

    /// <summary>Provider recurring reference (plan/subscription/token id).</summary>
    public string? ProviderRef { get; private set; }

    /// <summary>When created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When last updated (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Starts an active subscription for the current period.</summary>
    /// <param name="customerId">The customer id.</param>
    /// <param name="planId">The plan id.</param>
    /// <param name="periodStart">Period start (UTC).</param>
    /// <param name="periodEnd">Period end (UTC).</param>
    /// <param name="now">Creation timestamp (UTC).</param>
    /// <returns>The subscription, or a validation error.</returns>
    public static Result<Subscription> Start(Guid customerId, Guid planId, DateTimeOffset periodStart, DateTimeOffset periodEnd, DateTimeOffset now)
    {
        if (customerId == Guid.Empty)
        {
            return Error.Validation("subscription.customer_required", "A customer id is required.");
        }

        return new Subscription(Guid.NewGuid())
        {
            CustomerId = customerId,
            PlanId = planId,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = periodStart,
            CurrentPeriodEnd = periodEnd,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>Records the provider's recurring reference.</summary>
    /// <param name="providerRef">The reference.</param>
    /// <param name="now">Reference time (UTC).</param>
    public void SetProviderRef(string providerRef, DateTimeOffset now)
    {
        ProviderRef = providerRef;
        UpdatedAt = now;
    }

    /// <summary>Advances to the next billing period after a successful renewal charge.</summary>
    /// <param name="periodEnd">The new period end (UTC).</param>
    /// <param name="now">Reference time (UTC).</param>
    public void Renew(DateTimeOffset periodEnd, DateTimeOffset now)
    {
        CurrentPeriodStart = CurrentPeriodEnd;
        CurrentPeriodEnd = periodEnd;
        Status = SubscriptionStatus.Active;
        UpdatedAt = now;
    }

    /// <summary>Marks the subscription past-due after a failed renewal.</summary>
    /// <param name="now">Reference time (UTC).</param>
    public void MarkPastDue(DateTimeOffset now)
    {
        Status = SubscriptionStatus.PastDue;
        UpdatedAt = now;
    }

    /// <summary>Cancels the subscription (terminal).</summary>
    /// <param name="now">Reference time (UTC).</param>
    /// <returns>Success, or a conflict if already cancelled.</returns>
    public Result<Subscription> Cancel(DateTimeOffset now)
    {
        if (Status == SubscriptionStatus.Canceled)
        {
            return Error.Conflict("subscription.already_cancelled", "The subscription is already cancelled.");
        }

        Status = SubscriptionStatus.Canceled;
        UpdatedAt = now;
        return this;
    }
}
