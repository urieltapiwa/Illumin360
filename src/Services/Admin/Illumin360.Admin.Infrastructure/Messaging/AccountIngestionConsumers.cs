using Illumin360.Admin.Domain;
using Illumin360.Admin.Infrastructure.Persistence;
using Illumin360.Employers.IntegrationEvents;
using Illumin360.Professionals.IntegrationEvents;
using Illumin360.Students.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Admin.Infrastructure.Messaging;

/// <summary>
/// Ingests cross-service registration events into the Admin account directory so the platform
/// dashboard reflects real sign-ups, not just seeded rows. The external service id becomes the
/// account id, making re-delivery idempotent (the EF inbox also de-duplicates).
/// </summary>
internal static class AccountIngestion
{
    /// <summary>Upserts a talent account from a registration event.</summary>
    public static async Task UpsertTalentAsync(
        AdminDbContext db, Guid externalId, string fullName, DateTimeOffset registeredAt, CancellationToken ct)
    {
        var id = new AccountId(externalId);
        if (await db.Accounts.AnyAsync(a => a.Id == id, ct).ConfigureAwait(false))
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(fullName) ? "New talent" : fullName.Trim();
        db.Accounts.Add(AdminAccount.Seed(externalId, name, "Talent", email: string.Empty, region: "Unknown", createdAt: registeredAt));
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Upserts a company account from an employer registration event.</summary>
    public static async Task UpsertCompanyAsync(
        AdminDbContext db, Guid externalId, string companyName, string city, DateTimeOffset registeredAt, CancellationToken ct)
    {
        var id = new AccountId(externalId);
        if (await db.Accounts.AnyAsync(a => a.Id == id, ct).ConfigureAwait(false))
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(companyName) ? "New company" : companyName.Trim();
        var region = string.IsNullOrWhiteSpace(city) ? "Unknown" : city.Trim();
        db.Accounts.Add(AdminAccount.Seed(externalId, name, "Company", email: string.Empty, region: region, createdAt: registeredAt));
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>Adds a student sign-up to the Admin account directory as talent.</summary>
public sealed class StudentRegisteredConsumer(AdminDbContext db) : IConsumer<StudentRegistered>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<StudentRegistered> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var e = context.Message;
        await AccountIngestion.UpsertTalentAsync(db, e.StudentId, e.FullName, e.OccurredOn, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Adds a professional sign-up to the Admin account directory as talent.</summary>
public sealed class ProfessionalRegisteredConsumer(AdminDbContext db) : IConsumer<ProfessionalRegistered>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ProfessionalRegistered> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var e = context.Message;
        await AccountIngestion.UpsertTalentAsync(db, e.ProfessionalId, e.FullName, e.OccurredOn, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Adds an employer sign-up to the Admin account directory as a company.</summary>
public sealed class EmployerRegisteredConsumer(AdminDbContext db) : IConsumer<EmployerRegistered>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<EmployerRegistered> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var e = context.Message;
        await AccountIngestion.UpsertCompanyAsync(db, e.EmployerId, e.CompanyName, e.City, e.OccurredOn, context.CancellationToken).ConfigureAwait(false);
    }
}
