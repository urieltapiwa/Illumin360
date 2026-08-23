using Illumin360.Admin.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Admin.Infrastructure.Persistence;

/// <summary>
/// Seeds the Admin database with a demo verification queue on first run (idempotent). Real
/// verifications arrive from onboarding flows; this gives the Admin portal a live, actionable queue.
/// </summary>
public static class AdminSeeder
{
    private static readonly (string Id, string Entity, string Kind, string Submitted, string Risk)[] DemoQueue =
    [
        ("ad100001-0000-4000-8000-000000000001", "Etosha Consulting", "Company verification", "34m ago", "Low"),
        ("ad100001-0000-4000-8000-000000000002", "Apex Namibia", "Recruiter ID", "12m ago", "Low"),
        ("ad100001-0000-4000-8000-000000000003", "Meridian Group", "Company verification", "5h ago", "Medium"),
        ("ad100001-0000-4000-8000-000000000004", "Orange River Trading", "Document review", "3h ago", "Low"),
        ("ad100001-0000-4000-8000-000000000005", "Erongo Consulting", "Company verification", "3h ago", "Medium"),
        ("ad100001-0000-4000-8000-000000000006", "Khomas CC", "Recruiter ID", "34m ago", "Medium"),
    ];

    /// <summary>Inserts the demo verification queue if the database has none yet.</summary>
    /// <param name="db">The Admin database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when seeding is done (or skipped).</returns>
    public static async Task SeedAsync(AdminDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        var createdAt = new DateTimeOffset(2026, 8, 9, 6, 0, 0, TimeSpan.Zero);

        // Each collection is seeded independently and idempotently, so adding tickets/accounts in a later
        // phase still seeds them even though the verification queue was already seeded earlier.
        if (!await db.Verifications.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var (id, entity, kind, submitted, risk) in DemoQueue)
            {
                db.Verifications.Add(Verification.Seed(new Guid(id), entity, kind, risk, submitted, createdAt));
            }
        }

        if (!await db.Tickets.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            db.Tickets.AddRange(
            Ticket.Seed(new Guid("ad200001-0000-4000-8000-000000000001"), "Cannot upload CV document", "P1", "selma@nust.na", createdAt),
            Ticket.Seed(new Guid("ad200001-0000-4000-8000-000000000002"), "Employer billing invoice query", "P2", "accounts@baobab.na", createdAt),
            Ticket.Seed(new Guid("ad200001-0000-4000-8000-000000000003"), "Reset MFA for recruiter", "P2", "hr@unitygroup.na", createdAt),
            Ticket.Seed(new Guid("ad200001-0000-4000-8000-000000000004"), "Profile photo not saving", "P3", "panduleni@gmail.com", createdAt),
            Ticket.Seed(new Guid("ad200001-0000-4000-8000-000000000005"), "Feature request: bulk export", "P3", "ops@kalahari.na", createdAt),
            Ticket.SeedResolved(new Guid("ad200001-0000-4000-8000-000000000006"), "Password reset help", "P3", "info@erongotech.na", createdAt, createdAt.AddDays(6)),
            Ticket.SeedResolved(new Guid("ad200001-0000-4000-8000-000000000007"), "Verify company domain", "P2", "careers@unitygroup.na", createdAt.AddDays(2), createdAt.AddDays(9)));
        }

        if (!await db.Accounts.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            db.Accounts.AddRange(
                AdminAccount.Seed(new Guid("ad300001-0000-4000-8000-000000000001"), "Baobab (Pty) Ltd", "Company", "hr@baobab.na", "Windhoek", createdAt),
                AdminAccount.Seed(new Guid("ad300001-0000-4000-8000-000000000002"), "Selma Nghidinwa", "Talent", "selma@nust.na", "Windhoek", createdAt),
                AdminAccount.Seed(new Guid("ad300001-0000-4000-8000-000000000003"), "Unity Group", "Company", "careers@unitygroup.na", "Walvis Bay", createdAt),
                AdminAccount.Seed(new Guid("ad300001-0000-4000-8000-000000000004"), "Panduleni Amukwa", "Talent", "panduleni@gmail.com", "Oshakati", createdAt),
                AdminAccount.Seed(new Guid("ad300001-0000-4000-8000-000000000005"), "Kalahari CC", "Company", "ops@kalahari.na", "Swakopmund", createdAt),
                AdminAccount.Seed(new Guid("ad300001-0000-4000-8000-000000000006"), "Erongo Tech", "Company", "info@erongotech.na", "Windhoek", createdAt));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
