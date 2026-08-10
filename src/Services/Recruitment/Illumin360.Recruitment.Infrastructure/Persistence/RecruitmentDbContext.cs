using Illumin360.Recruitment.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Illumin360.Recruitment.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Recruitment service. Owns the <c>recruitment</c> bounded-context database
/// (charter Part 13: database-per-service). The <c>recruitment_requests</c> and <c>applications</c>
/// tables are pre-existing (externally seeded with a decade of history), so they are mapped for
/// query/write but excluded from migrations — only the MassTransit outbox tables are migration-managed.
/// </summary>
public sealed class RecruitmentDbContext(DbContextOptions<RecruitmentDbContext> options) : DbContext(options)
{
    /// <summary>The recruitment request aggregate set.</summary>
    public DbSet<RecruitmentRequest> Requests => Set<RecruitmentRequest>();

    /// <summary>The applications read-model set.</summary>
    public DbSet<RecruitmentApplication> Applications => Set<RecruitmentApplication>();

    /// <summary>Talent saved-searches set (owned + migration-managed by this service).</summary>
    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();

    /// <summary>Interviews set (owned + migration-managed by this service).</summary>
    public DbSet<Interview> Interviews => Set<Interview>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("recruitment");

        // MassTransit transactional outbox tables (inbox/outbox state + outbox messages). These are the
        // only tables this service creates; they are added via the startup migration.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        // The seeded status column stores lowercase values ("open"/"filled"); map explicitly both ways
        // (avoids ToLowerInvariant / CA1308 and keeps the round-trip exact).
        var statusConverter = new ValueConverter<RequestStatus, string>(
            v => v == RequestStatus.Filled ? "filled" : v == RequestStatus.Closed ? "closed" : "open",
            v => v == "filled" ? RequestStatus.Filled : v == "closed" ? RequestStatus.Closed : RequestStatus.Open);

        modelBuilder.Entity<RecruitmentRequest>(b =>
        {
            // Pre-existing, externally-seeded table — mapped but never created/altered by migrations.
            b.ToTable("recruitment_requests", t => t.ExcludeFromMigrations());
            b.HasKey(r => r.Id);
            b.Property(r => r.Id)
                .HasColumnName("id")
                .HasConversion(id => id.Value, value => new RequestId(value));
            b.Property(r => r.CompanyId).HasColumnName("company_id");
            b.Property(r => r.Title).HasColumnName("title").HasMaxLength(150);
            b.Property(r => r.City).HasColumnName("city").HasMaxLength(100);
            b.Property(r => r.Positions).HasColumnName("positions");
            b.Property(r => r.Status).HasColumnName("status").HasConversion(statusConverter).HasMaxLength(20);
            b.Property(r => r.CreatedAt).HasColumnName("created_at");
            b.Property(r => r.FilledAt).HasColumnName("filled_at");
            b.Ignore(r => r.DomainEvents);
        });

        modelBuilder.Entity<SavedSearch>(b =>
        {
            // Owned by this service — created/altered by migrations (unlike requests/applications).
            b.ToTable("saved_searches");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id)
                .HasColumnName("id")
                .HasConversion(id => id.Value, value => new SavedSearchId(value));
            b.Property(s => s.TalentId).HasColumnName("talent_id");
            b.Property(s => s.Label).HasColumnName("label").HasMaxLength(120);
            b.Property(s => s.City).HasColumnName("city").HasMaxLength(100);
            b.Property(s => s.Keyword).HasColumnName("keyword").HasMaxLength(120);
            b.Property(s => s.AlertsEnabled).HasColumnName("alerts_enabled");
            b.Property(s => s.CreatedAt).HasColumnName("created_at");
            b.HasIndex(s => s.TalentId);
            b.Ignore(s => s.DomainEvents);
        });

        modelBuilder.Entity<Interview>(b =>
        {
            b.ToTable("interviews");
            b.HasKey(i => i.Id);
            b.Property(i => i.Id)
                .HasColumnName("id")
                .HasConversion(id => id.Value, value => new InterviewId(value));
            b.Property(i => i.ApplicationId).HasColumnName("application_id");
            b.Property(i => i.ScheduledAt).HasColumnName("scheduled_at");
            b.Property(i => i.DurationMinutes).HasColumnName("duration_minutes");
            b.Property(i => i.Location).HasColumnName("location").HasMaxLength(200);
            b.Property(i => i.Status).HasColumnName("status").HasMaxLength(20);
            b.Property(i => i.FeedbackRating).HasColumnName("feedback_rating");
            b.Property(i => i.FeedbackComment).HasColumnName("feedback_comment").HasMaxLength(1000);
            b.Property(i => i.CreatedAt).HasColumnName("created_at");
            b.HasIndex(i => i.ApplicationId);
            b.Ignore(i => i.DomainEvents);
        });

        modelBuilder.Entity<RecruitmentApplication>(b =>
        {
            b.ToTable("applications", t => t.ExcludeFromMigrations());
            b.HasKey(a => a.Id);
            b.Property(a => a.Id)
                .HasColumnName("id")
                .HasConversion(id => id.Value, value => new Illumin360.Recruitment.Domain.ApplicationId(value));
            b.Property(a => a.RequestId)
                .HasColumnName("request_id")
                .HasConversion(id => id.Value, value => new RequestId(value));
            b.Property(a => a.TalentId).HasColumnName("talent_id");
            b.Property(a => a.TalentType).HasColumnName("talent_type").HasMaxLength(20);
            b.Property(a => a.MatchScore).HasColumnName("match_score").HasPrecision(5, 2);
            b.Property(a => a.Status).HasColumnName("status").HasMaxLength(20);
            b.Property(a => a.IsHire).HasColumnName("is_hire");
            b.Property(a => a.AppliedAt).HasColumnName("applied_at");
            b.Property(a => a.DecidedAt).HasColumnName("decided_at");
            b.Ignore(a => a.DomainEvents);
        });
    }
}
