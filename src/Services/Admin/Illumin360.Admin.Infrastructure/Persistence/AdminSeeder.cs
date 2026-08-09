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

        if (await db.Verifications.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var createdAt = new DateTimeOffset(2026, 8, 9, 6, 0, 0, TimeSpan.Zero);
        foreach (var (id, entity, kind, submitted, risk) in DemoQueue)
        {
            db.Verifications.Add(Verification.Seed(new Guid(id), entity, kind, risk, submitted, createdAt));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
