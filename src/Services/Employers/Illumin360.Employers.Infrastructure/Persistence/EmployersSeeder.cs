using Illumin360.Employers.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Employers.Infrastructure.Persistence;

/// <summary>Seeds a demo employer on first run (idempotent) so the portal has live data out of the box.</summary>
public static class EmployersSeeder
{
    private static readonly Guid DemoEmployerId = new("e3b0c000-0000-4000-8000-000000000001");

    /// <summary>Inserts the demo employer if none exist.</summary>
    /// <param name="db">The Employers database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when seeding is done (or skipped).</returns>
    public static async Task SeedAsync(EmployersDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (await db.Employers.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        db.Employers.Add(Employer.Seed(
            DemoEmployerId,
            companyName: "Namib Mills",
            industry: "Manufacturing",
            city: "Windhoek",
            website: "https://namibmills.com.na",
            about: "One of Namibia's largest food producers, hiring across operations, engineering and finance.",
            createdAt: new DateTimeOffset(2023, 1, 15, 0, 0, 0, TimeSpan.Zero)));

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
