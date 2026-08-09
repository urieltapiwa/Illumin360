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

    /// <summary>The support-ticket set.</summary>
    public DbSet<Ticket> Tickets => Set<Ticket>();

    /// <summary>The account-directory set.</summary>
    public DbSet<AdminAccount> Accounts => Set<AdminAccount>();

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

        var ticketStatusConverter = new ValueConverter<TicketStatus, string>(
            v => v == TicketStatus.Assigned ? "assigned" : v == TicketStatus.Resolved ? "resolved" : "open",
            v => v == "assigned" ? TicketStatus.Assigned : v == "resolved" ? TicketStatus.Resolved : TicketStatus.Open);

        modelBuilder.Entity<Ticket>(b =>
        {
            b.ToTable("tickets");
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).HasColumnName("id").HasConversion(id => id.Value, value => new TicketId(value));
            b.Property(t => t.Subject).HasColumnName("subject").HasMaxLength(200);
            b.Property(t => t.Priority).HasColumnName("priority").HasMaxLength(8);
            b.Property(t => t.Requester).HasColumnName("requester").HasMaxLength(160);
            b.Property(t => t.Status).HasColumnName("status").HasConversion(ticketStatusConverter).HasMaxLength(20);
            b.Property(t => t.Assignee).HasColumnName("assignee").HasMaxLength(120);
            b.Property(t => t.CreatedAt).HasColumnName("created_at");
            b.HasIndex(t => t.Status);
            b.Ignore(t => t.DomainEvents);
        });

        var accountStatusConverter = new ValueConverter<AccountStatus, string>(
            v => v == AccountStatus.Suspended ? "suspended" : "active",
            v => v == "suspended" ? AccountStatus.Suspended : AccountStatus.Active);

        modelBuilder.Entity<AdminAccount>(b =>
        {
            b.ToTable("accounts");
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).HasColumnName("id").HasConversion(id => id.Value, value => new AccountId(value));
            b.Property(a => a.Name).HasColumnName("name").HasMaxLength(160);
            b.Property(a => a.Kind).HasColumnName("kind").HasMaxLength(20);
            b.Property(a => a.Email).HasColumnName("email").HasMaxLength(160);
            b.Property(a => a.Status).HasColumnName("status").HasConversion(accountStatusConverter).HasMaxLength(20);
            b.Property(a => a.CreatedAt).HasColumnName("created_at");
            b.HasIndex(a => a.Status);
            b.Ignore(a => a.DomainEvents);
        });
    }
}
