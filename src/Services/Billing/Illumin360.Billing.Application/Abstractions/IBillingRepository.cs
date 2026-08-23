using Illumin360.Billing.Domain;

namespace Illumin360.Billing.Application.Abstractions;

/// <summary>Port for Billing persistence (ports &amp; adapters — charter Part 5).</summary>
public interface IBillingRepository
{
    /// <summary>Stages a new plan.</summary>
    /// <param name="plan">The plan.</param>
    void AddPlan(Plan plan);

    /// <summary>Loads a plan by id, or null.</summary>
    /// <param name="id">The plan id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Plan?> GetPlanAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Loads a plan by its code, or null.</summary>
    /// <param name="code">The plan code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Plan?> GetPlanByCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>Lists active plans.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Plan>> ListPlansAsync(CancellationToken cancellationToken);

    /// <summary>Stages a new subscription.</summary>
    /// <param name="subscription">The subscription.</param>
    void AddSubscription(Subscription subscription);

    /// <summary>Loads a subscription by id (change-tracked), or null.</summary>
    /// <param name="id">The subscription id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Subscription?> GetSubscriptionAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Loads a customer's non-cancelled subscription (change-tracked), or null.</summary>
    /// <param name="customerId">The customer id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Subscription?> GetActiveSubscriptionForCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    /// <summary>Lists subscriptions whose current period has ended and are due to be charged (change-tracked).</summary>
    /// <param name="now">Reference time (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Subscription>> ListDueSubscriptionsAsync(DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Lists every subscription (all statuses), for reporting/analytics.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Subscription>> ListAllSubscriptionsAsync(CancellationToken cancellationToken);

    /// <summary>Stages a new invoice.</summary>
    /// <param name="invoice">The invoice.</param>
    void AddInvoice(Invoice invoice);

    /// <summary>Lists a subscription's invoices, newest first.</summary>
    /// <param name="subscriptionId">The subscription id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Invoice>> ListInvoicesForSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken);

    /// <summary>Commits staged changes.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
