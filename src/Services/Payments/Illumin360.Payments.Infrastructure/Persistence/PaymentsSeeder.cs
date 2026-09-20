using Illumin360.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Payments.Infrastructure.Persistence;

/// <summary>
/// Seeds a handful of demo contracts + milestones on first run (idempotent) so the Employer portal's
/// Contracts page has live data out of the box. Contracts belong to the demo employer
/// (<c>e3b0c000-…-0001</c>, the account returned by GET /api/employers/me).
/// </summary>
public static class PaymentsSeeder
{
    // Matches EmployersSeeder.DemoEmployerId — the client the Employer portal acts as.
    private static readonly Guid DemoClientId = new("e3b0c000-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Seeded = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Inserts demo contracts if the contracts table is empty.</summary>
    /// <param name="db">The Payments database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when seeding is done (or skipped).</returns>
    public static async Task SeedAsync(PaymentsDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (await db.Contracts.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        // 1) Active contract with three milestones.
        AddContract(db, new Guid("7a1e0000-0000-4000-8000-000000000001"), "Website Redesign", activate: true, cancel: false, new[] { ("Discovery & wireframes", 1_800_000L), ("Design system", 2_500_000L), ("Build & launch", 3_200_000L) });

        // 2) Draft contract still being scoped.
        AddContract(db, new Guid("7a1e0000-0000-4000-8000-000000000002"), "Mobile App Build", activate: false, cancel: false, new[] { ("MVP scope", 4_000_000L), ("Play Store release", 2_000_000L) });

        // 3) Cancelled engagement.
        AddContract(db, new Guid("7a1e0000-0000-4000-8000-000000000003"), "Brand Identity Package", activate: false, cancel: true, new[] { ("Logo & guidelines", 1_200_000L) });

        // 4) Active retainer with two milestones.
        AddContract(db, new Guid("7a1e0000-0000-4000-8000-000000000004"), "SEO Retainer (Q3)", activate: true, cancel: false, new[] { ("July optimisation", 900_000L), ("August optimisation", 900_000L) });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddContract(
        PaymentsDbContext db,
        Guid talentId,
        string title,
        bool activate,
        bool cancel,
        (string Title, long AmountMinor)[] milestones)
    {
        var created = Contract.Create(DemoClientId, talentId, requestId: null, title, "NAD", Seeded);
        if (!created.IsSuccess)
        {
            return;
        }

        var contract = created.Value!;
        db.Contracts.Add(contract);

        var order = 1;
        var count = 0;
        foreach (var (mTitle, amount) in milestones)
        {
            if (string.IsNullOrEmpty(mTitle))
            {
                continue;
            }

            var m = Milestone.Create(contract.Id, order, mTitle, amount, Seeded);
            if (m.IsSuccess)
            {
                db.Milestones.Add(m.Value!);
                order++;
                count++;
            }
        }

        if (activate)
        {
            contract.Activate(count, Seeded);
        }
        else if (cancel)
        {
            contract.Cancel(Seeded);
        }
    }
}
