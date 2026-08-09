using Illumin360.Admin.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Illumin360.Admin.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Admin service (verification queue, and — in later phases — tickets and
/// user accounts). Owns the <c>admin</c> bounded-context database (charter Part 13) and
/// migration-manages all of its tables alongside the MassTransit transactional outbox.
/// </summary>
public sealed class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    /// <summary>The verification-queue set.</summary>
    public DbSet<Verification> Verifications => Set<Verification>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("admin");

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        // Map the status enum to lowercase strings explicitly (avoids ToLowerInvariant / CA1308).
        var statusConverter = new ValueConverter<VerificationStatus, string>(
            v => v == VerificationStatus.Approved ? "approved" : v == VerificationStatus.Rejected ? "rejected" : "pending",
            v => v == "approved" ? VerificationStatus.Approved : v == "rejected" ? VerificationStatus.Rejected : VerificationStatus.Pending);

        modelBuilder.Entity<Verification>(b =>
        {
            b.ToTable("verifications");
            b.HasKey(v => v.Id);
            b.Property(v => v.Id)
                .HasColumnName("id")
                .HasConversion(id => id.Value, value => new VerificationId(value));
            b.Property(v => v.Entity).HasColumnName("entity").HasMaxLength(160);
            b.Property(v => v.Kind).HasColumnName("kind").HasMaxLength(80);
            b.Property(v => v.RiskLevel).HasColumnName("risk_level").HasMaxLength(20);
            b.Property(v => v.SubmittedLabel).HasColumnName("submitted_label").HasMaxLength(40);
            b.Property(v => v.Status).HasColumnName("status").HasConversion(statusConverter).HasMaxLength(20);
            b.Property(v => v.DecidedBy).HasColumnName("decided_by").HasMaxLength(120);
            b.Property(v => v.DecidedAt).HasColumnName("decided_at");
            b.Property(v => v.CreatedAt).HasColumnName("created_at");
            b.HasIndex(v => v.Status);
            b.Ignore(v => v.DomainEvents);
        });
    }
}
