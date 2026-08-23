using Illumin360.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Billing.Infrastructure.Persistence;

/// <summary>
/// Seeds the Billing database on first run (idempotent) with three NAD plans and a set of subscriptions
/// whose created-dates are spread over the last six months, so the platform MRR trend has real, rising data.
/// </summary>
public static class BillingSeeder
{
    // (monthsAgo, plan-picker) — subscriptions accumulate month by month into a rising MRR curve.
    private static readonly (int MonthsAgo, int PlanIndex)[] Schedule =
    [
        (5, 0), (4, 1), (4, 0), (3, 1), (2, 2), (2, 1), (1, 0), (1, 1), (0, 2),
    ];

    /// <summary>Inserts the demo plans and subscriptions if the database has none yet.</summary>
    /// <param name="db">The billing database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when seeding is done (or skipped).</returns>
    public static async Task SeedAsync(BillingDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (await db.Plans.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var plans = new[]
        {
            Plan.Create("starter", "Starter", 150000, "NAD", BillingInterval.Monthly, ["basic"], now).Value!,
            Plan.Create("pro", "Pro", 500000, "NAD", BillingInterval.Monthly, ["basic", "advanced"], now).Value!,
            Plan.Create("enterprise", "Enterprise", 1500000, "NAD", BillingInterval.Monthly, ["basic", "advanced", "priority"], now).Value!,
        };
        db.Plans.AddRange(plans);

        var firstOfThisMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
        foreach (var (monthsAgo, planIndex) in Schedule)
        {
            var created = firstOfThisMonth.AddMonths(-monthsAgo).AddDays(3);
            var sub = Subscription.Start(Guid.NewGuid(), plans[planIndex].Id, created, created.AddMonths(1), created);
            if (sub.IsSuccess)
            {
                db.Subscriptions.Add(sub.Value!);
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
