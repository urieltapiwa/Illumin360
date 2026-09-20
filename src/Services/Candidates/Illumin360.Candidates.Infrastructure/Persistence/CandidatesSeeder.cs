using Illumin360.Candidates.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Candidates.Infrastructure.Persistence;

/// <summary>
/// Seeds a demo talent pool on first run (idempotent) so the anonymous <c>/stats</c> endpoint returns
/// realistic, non-zero city/availability breakdowns for the employer dashboard. Deterministic: candidates
/// are created through the <see cref="Candidate.Register(string, string, string, string, AvailabilityStatus, string?)"/>
/// factory with fixed inputs and no clock/randomness in this seeder.
/// </summary>
public static class CandidatesSeeder
{
    // One row per candidate.
    // Fields: first, last, city, nationality, availability, headline. Spread across five
    // Namibian cities and all three availability states so ByCity / ByAvailability are non-trivial.
    private static readonly (string First, string Last, string City, string Nationality, AvailabilityStatus Availability, string Headline)[] Seed =
    [
        ("Selma", "Nghikembua", "Windhoek", "Namibian", AvailabilityStatus.ActivelyLooking, "Senior .NET engineer, 8 yrs fintech."),
        ("Petrus", "Amunyela", "Windhoek", "Namibian", AvailabilityStatus.OpenToOpportunities, "Product manager, marketplace platforms."),
        ("Johanna", "Shikongo", "Windhoek", "Namibian", AvailabilityStatus.ActivelyLooking, "UX designer with research background."),
        ("Tangeni", "Iipinge", "Windhoek", "Namibian", AvailabilityStatus.NotAvailable, "DevOps lead, Kubernetes + Azure."),
        ("Ndapewa", "Haufiku", "Windhoek", "Namibian", AvailabilityStatus.OpenToOpportunities, "Data analyst, SQL and Power BI."),
        ("Erastus", "Kandjii", "Windhoek", "Namibian", AvailabilityStatus.ActivelyLooking, "Backend developer, Java and Go."),
        ("Loide", "Nakale", "Windhoek", "Namibian", AvailabilityStatus.ActivelyLooking, "Graduate software engineer."),
        ("Gerson", "Uugwanga", "Windhoek", "Namibian", AvailabilityStatus.OpenToOpportunities, "QA automation engineer."),

        // Walvis Bay (5)
        ("Maria", "Nangolo", "Walvis Bay", "Namibian", AvailabilityStatus.ActivelyLooking, "Logistics coordinator, port operations."),
        ("Fillemon", "Shivute", "Walvis Bay", "Namibian", AvailabilityStatus.OpenToOpportunities, "Supply-chain analyst."),
        ("Aina", "Namundjebo", "Walvis Bay", "Namibian", AvailabilityStatus.ActivelyLooking, "Mechanical technician, marine."),
        ("Simeon", "Kauluma", "Walvis Bay", "Namibian", AvailabilityStatus.NotAvailable, "Warehouse operations supervisor."),
        ("Rauna", "Angula", "Walvis Bay", "Namibian", AvailabilityStatus.OpenToOpportunities, "HR generalist, industrial sector."),

        // Swakopmund (4)
        ("Hendrik", "Beukes", "Swakopmund", "Namibian", AvailabilityStatus.ActivelyLooking, "Frontend developer, React/TypeScript."),
        ("Elizabeth", "Gawises", "Swakopmund", "Namibian", AvailabilityStatus.OpenToOpportunities, "Hospitality manager, tourism."),
        ("Immanuel", "//Hoëb", "Swakopmund", "Namibian", AvailabilityStatus.ActivelyLooking, "Tour operations coordinator."),
        ("Sophia", "Tjihero", "Swakopmund", "Namibian", AvailabilityStatus.NotAvailable, "Marketing lead, coastal tourism."),

        // Oshakati (4)
        ("Nangula", "Ndeitunga", "Oshakati", "Namibian", AvailabilityStatus.ActivelyLooking, "Registered nurse, primary care."),
        ("Josef", "Mwatilifange", "Oshakati", "Namibian", AvailabilityStatus.OpenToOpportunities, "Field sales representative."),
        ("Victoria", "Amukwaya", "Oshakati", "Namibian", AvailabilityStatus.ActivelyLooking, "Accountant, retail chains."),
        ("Paulus", "Nakanyala", "Oshakati", "Namibian", AvailabilityStatus.ActivelyLooking, "Electrician, commercial installs."),

        // Rundu (3)
        ("Kaarina", "Sikongo", "Rundu", "Namibian", AvailabilityStatus.OpenToOpportunities, "Agronomist, irrigation projects."),
        ("Matheus", "Haingura", "Rundu", "Namibian", AvailabilityStatus.ActivelyLooking, "Civil engineering technician."),
        ("Ester", "Mukoya", "Rundu", "Namibian", AvailabilityStatus.NotAvailable, "Community-health coordinator."),
    ];

    /// <summary>Inserts the demo candidate pool if the candidates table is empty.</summary>
    /// <param name="db">The Candidates database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when seeding is done (or skipped).</returns>
    public static async Task SeedAsync(CandidatesDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (await db.Candidates.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        foreach (var row in Seed)
        {
            var result = Candidate.Register(row.First, row.Last, row.City, row.Nationality, row.Availability, row.Headline);
            if (result.IsSuccess)
            {
                db.Candidates.Add(result.Value!);
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
