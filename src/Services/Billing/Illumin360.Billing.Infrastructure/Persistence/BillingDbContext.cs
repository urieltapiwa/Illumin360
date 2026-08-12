using Illumin360.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Billing.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Billing service — the platform's own SaaS revenue (plans, subscriptions, invoices).
/// Owns the <c>billing</c> bounded-context database (database-per-service). Money is integer minor units +
/// ISO-4217 currency. Distinct from the marketplace <c>Payments</c> service (client↔talent escrow).
/// </summary>
public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    /// <summary>Pricing plans.</summary>
    public DbSet<Plan> Plans => Set<Plan>();

    /// <summary>Subscriptions.</summary>
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    /// <summary>Invoices.</summary>
    public DbSet<Invoice> Invoices => Set<Invoice>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("billing");

        modelBuilder.Entity<Plan>(b =>
        {
            b.ToTable("plans");
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).HasColumnName("id");
            b.Property(p => p.Code).HasColumnName("code").HasMaxLength(40);
            b.Property(p => p.Name).HasColumnName("name").HasMaxLength(120);
            b.Property(p => p.PriceMinor).HasColumnName("price_minor");
            b.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3);
            b.Property(p => p.Interval).HasColumnName("interval").HasMaxLength(20).HasConversion(v => v.ToString(), s => Enum.Parse<BillingInterval>(s));
            b.Property(p => p.FeaturesCsv).HasColumnName("features").HasMaxLength(2000);
            b.Property(p => p.Active).HasColumnName("active");
            b.Property(p => p.CreatedAt).HasColumnName("created_at");
            b.HasIndex(p => p.Code).IsUnique();
            b.Ignore(p => p.Features);
            b.Ignore(p => p.DomainEvents);
        });

        modelBuilder.Entity<Subscription>(b =>
        {
            b.ToTable("subscriptions");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).HasColumnName("id");
            b.Property(s => s.CustomerId).HasColumnName("customer_id");
            b.Property(s => s.PlanId).HasColumnName("plan_id");
            b.Property(s => s.Status).HasColumnName("status").HasMaxLength(20).HasConversion(v => v.ToString(), s => Enum.Parse<SubscriptionStatus>(s));
            b.Property(s => s.CurrentPeriodStart).HasColumnName("current_period_start");
            b.Property(s => s.CurrentPeriodEnd).HasColumnName("current_period_end");
            b.Property(s => s.ProviderRef).HasColumnName("provider_ref").HasMaxLength(200);
            b.Property(s => s.CreatedAt).HasColumnName("created_at");
            b.Property(s => s.UpdatedAt).HasColumnName("updated_at");
            b.HasIndex(s => s.CustomerId);
            b.HasIndex(s => new { s.Status, s.CurrentPeriodEnd });
            b.Ignore(s => s.DomainEvents);
        });

        modelBuilder.Entity<Invoice>(b =>
        {
            b.ToTable("invoices");
            b.HasKey(i => i.Id);
            b.Property(i => i.Id).HasColumnName("id");
            b.Property(i => i.SubscriptionId).HasColumnName("subscription_id");
            b.Property(i => i.AmountMinor).HasColumnName("amount_minor");
            b.Property(i => i.Currency).HasColumnName("currency").HasMaxLength(3);
            b.Property(i => i.Status).HasColumnName("status").HasMaxLength(20).HasConversion(v => v.ToString(), s => Enum.Parse<InvoiceStatus>(s));
            b.Property(i => i.PeriodStart).HasColumnName("period_start");
            b.Property(i => i.PeriodEnd).HasColumnName("period_end");
            b.Property(i => i.IssuedAt).HasColumnName("issued_at");
            b.Property(i => i.PaidAt).HasColumnName("paid_at");
            b.Property(i => i.ProviderRef).HasColumnName("provider_ref").HasMaxLength(200);
            b.HasIndex(i => i.SubscriptionId);
            b.Ignore(i => i.DomainEvents);
        });
    }
}
