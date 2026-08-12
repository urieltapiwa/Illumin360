using Illumin360.Billing.Application.Abstractions;
using Illumin360.Billing.Domain;

namespace Illumin360.Billing.Application.Billing;

/// <summary>
/// Charges due subscription renewals: for each active subscription whose current period has ended, issues an
/// invoice, charges the provider against the stored recurring reference, and either renews (paid) or marks the
/// subscription past-due (failed). Deterministic; safe to run on a timer. Dunning/retry beyond marking
/// past-due is a follow-up.
/// </summary>
/// <param name="repository">The billing repository.</param>
/// <param name="provider">The billing provider.</param>
public sealed class BillingRunner(IBillingRepository repository, IBillingProvider provider)
{
    private readonly IBillingRepository _repository = repository;
    private readonly IBillingProvider _provider = provider;

    /// <summary>Runs one pass: charges every due renewal.</summary>
    /// <param name="now">Reference time (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of successful renewal charges.</returns>
    public async Task<int> RunOnceAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var due = await _repository.ListDueSubscriptionsAsync(now, cancellationToken).ConfigureAwait(false);
        if (due.Count == 0)
        {
            return 0;
        }

        var planCache = new Dictionary<Guid, Plan?>();
        var charged = 0;

        foreach (var subscription in due)
        {
            if (!planCache.TryGetValue(subscription.PlanId, out var plan))
            {
                plan = await _repository.GetPlanAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
                planCache[subscription.PlanId] = plan;
            }

            if (plan is null)
            {
                continue;
            }

            var invoice = Invoice.Issue(subscription.Id, plan.PriceMinor, plan.Currency, subscription.CurrentPeriodEnd, plan.NextPeriodEnd(subscription.CurrentPeriodEnd), now);
            var result = await _provider.ChargeAsync(invoice.Id.ToString(), subscription.ProviderRef ?? string.Empty, plan.PriceMinor, plan.Currency, cancellationToken).ConfigureAwait(false);

            if (result.Success)
            {
                invoice.MarkPaid(result.Reference, now);
                subscription.Renew(plan.NextPeriodEnd(subscription.CurrentPeriodEnd), now);
                charged++;
            }
            else
            {
                invoice.MarkFailed();
                subscription.MarkPastDue(now);
            }

            _repository.AddInvoice(invoice);
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return charged;
    }
}
