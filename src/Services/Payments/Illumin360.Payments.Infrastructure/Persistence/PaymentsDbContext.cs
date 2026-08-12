using Illumin360.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Payments.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Payments service. Owns the <c>payments</c> bounded-context database
/// (database-per-service — charter Part 13); all tables are migration-managed. Money is stored as integer
/// minor units + ISO-4217 currency, never floats.
/// </summary>
public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    /// <summary>Contracts.</summary>
    public DbSet<Contract> Contracts => Set<Contract>();

    /// <summary>Milestones.</summary>
    public DbSet<Milestone> Milestones => Set<Milestone>();

    /// <summary>Ledger movements (append-only audit).</summary>
    public DbSet<LedgerMovement> LedgerMovements => Set<LedgerMovement>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("payments");

        modelBuilder.Entity<Contract>(b =>
        {
            b.ToTable("contracts");
            b.HasKey(c => c.Id);
            b.Property(c => c.Id).HasColumnName("id").HasConversion(id => id.Value, value => new ContractId(value));
            b.Property(c => c.ClientId).HasColumnName("client_id");
            b.Property(c => c.TalentId).HasColumnName("talent_id");
            b.Property(c => c.RequestId).HasColumnName("request_id");
            b.Property(c => c.Title).HasColumnName("title").HasMaxLength(160);
            b.Property(c => c.Type).HasColumnName("type").HasMaxLength(20).HasConversion(v => v.ToString(), s => Enum.Parse<ContractType>(s));
            b.Property(c => c.Currency).HasColumnName("currency").HasMaxLength(3);
            b.Property(c => c.Status).HasColumnName("status").HasMaxLength(20).HasConversion(v => v.ToString(), s => Enum.Parse<ContractStatus>(s));
            b.Property(c => c.CreatedAt).HasColumnName("created_at");
            b.Property(c => c.UpdatedAt).HasColumnName("updated_at");
            b.HasIndex(c => c.ClientId);
            b.HasIndex(c => c.TalentId);
            b.Ignore(c => c.DomainEvents);
        });

        modelBuilder.Entity<Milestone>(b =>
        {
            b.ToTable("milestones");
            b.HasKey(m => m.Id);
            b.Property(m => m.Id).HasColumnName("id").HasConversion(id => id.Value, value => new MilestoneId(value));
            b.Property(m => m.ContractId).HasColumnName("contract_id").HasConversion(id => id.Value, value => new ContractId(value));
            b.Property(m => m.Order).HasColumnName("order");
            b.Property(m => m.Title).HasColumnName("title").HasMaxLength(160);
            b.Property(m => m.AmountMinor).HasColumnName("amount_minor");
            b.Property(m => m.Status).HasColumnName("status").HasMaxLength(20).HasConversion(v => v.ToString(), s => Enum.Parse<MilestoneStatus>(s));
            b.Property(m => m.HoldReference).HasColumnName("hold_reference").HasMaxLength(200);
            b.Property(m => m.CreatedAt).HasColumnName("created_at");
            b.Property(m => m.FundedAt).HasColumnName("funded_at");
            b.Property(m => m.SubmittedAt).HasColumnName("submitted_at");
            b.Property(m => m.DecidedAt).HasColumnName("decided_at");
            b.HasIndex(m => m.ContractId);
            b.Ignore(m => m.DomainEvents);
        });

        modelBuilder.Entity<LedgerMovement>(b =>
        {
            b.ToTable("ledger_movements");
            b.HasKey(l => l.Id);
            b.Property(l => l.Id).HasColumnName("id");
            b.Property(l => l.ContractId).HasColumnName("contract_id").HasConversion(id => id.Value, value => new ContractId(value));
            b.Property(l => l.MilestoneId).HasColumnName("milestone_id").HasConversion(id => id.Value, value => new MilestoneId(value));
            b.Property(l => l.Kind).HasColumnName("kind").HasMaxLength(20).HasConversion(v => v.ToString(), s => Enum.Parse<MovementKind>(s));
            b.Property(l => l.AmountMinor).HasColumnName("amount_minor");
            b.Property(l => l.Currency).HasColumnName("currency").HasMaxLength(3);
            b.Property(l => l.ProviderReference).HasColumnName("provider_reference").HasMaxLength(200);
            b.Property(l => l.CreatedAt).HasColumnName("created_at");
            b.HasIndex(l => l.ContractId);
            b.Ignore(l => l.DomainEvents);
        });
    }
}
