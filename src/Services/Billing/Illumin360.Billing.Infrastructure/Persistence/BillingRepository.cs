using Illumin360.Billing.Application.Abstractions;
using Illumin360.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Billing.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IBillingRepository"/>.</summary>
/// <param name="db">The billing database context.</param>
public sealed class BillingRepository(BillingDbContext db) : IBillingRepository
{
    private readonly BillingDbContext _db = db;

    /// <inheritdoc />
    public void AddPlan(Plan plan) => _db.Plans.Add(plan);

    /// <inheritdoc />
    public async Task<Plan?> GetPlanAsync(Guid id, CancellationToken cancellationToken)
        => await _db.Plans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Plan?> GetPlanByCodeAsync(string code, CancellationToken cancellationToken)
        => await _db.Plans.FirstOrDefaultAsync(p => p.Code == code, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Plan>> ListPlansAsync(CancellationToken cancellationToken)
        => await _db.Plans.AsNoTracking().Where(p => p.Active).OrderBy(p => p.PriceMinor).ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddSubscription(Subscription subscription) => _db.Subscriptions.Add(subscription);

    /// <inheritdoc />
    public async Task<Subscription?> GetSubscriptionAsync(Guid id, CancellationToken cancellationToken)
        => await _db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Subscription?> GetActiveSubscriptionForCustomerAsync(Guid customerId, CancellationToken cancellationToken)
        => await _db.Subscriptions
            .Where(s => s.CustomerId == customerId && s.Status != SubscriptionStatus.Canceled)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Subscription>> ListDueSubscriptionsAsync(DateTimeOffset now, CancellationToken cancellationToken)
        => await _db.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active && s.CurrentPeriodEnd <= now)
            .OrderBy(s => s.CurrentPeriodEnd)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddInvoice(Invoice invoice) => _db.Invoices.Add(invoice);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Invoice>> ListInvoicesForSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
        => await _db.Invoices.AsNoTracking()
            .Where(i => i.SubscriptionId == subscriptionId)
            .OrderByDescending(i => i.IssuedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
}
