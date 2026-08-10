using Illumin360.Recruitment.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Recruitment.Infrastructure.Persistence;

/// <summary>Seeds a couple of demo CRM clients on first run (idempotent) so the recruiter CRM has data.</summary>
public static class CrmSeeder
{
    private static readonly Guid AcmeId = new("c1a11000-0000-4000-8000-000000000001");
    private static readonly Guid NamibId = new("c1a11000-0000-4000-8000-000000000002");
    private static readonly DateTimeOffset Seeded = new(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Inserts demo clients + a contact if the clients table is empty.</summary>
    /// <param name="db">The Recruitment database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when seeding is done (or skipped).</returns>
    public static async Task SeedAsync(RecruitmentDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (await db.Clients.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        db.Clients.Add(Client.Seed(AcmeId, "Acme Logistics", "Logistics", "Walvis Bay", ClientStatus.Active, "Repeat client — ships roles quarterly.", Seeded));
        db.Clients.Add(Client.Seed(NamibId, "Etosha Fintech", "Financial services", "Windhoek", ClientStatus.Prospect, "Warm intro via a portfolio company.", Seeded));

        db.ClientContacts.Add(ClientContact.Create(new ClientId(AcmeId), "Maria Nangolo", "Head of Talent", "maria@acmelogistics.na", "+264 64 200 100", true, Seeded).Value!);
        db.ClientContacts.Add(ClientContact.Create(new ClientId(NamibId), "Tomas Amupolo", "COO", "tomas@etoshafintech.na", null, true, Seeded).Value!);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
