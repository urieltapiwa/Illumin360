using Illumin360.Professionals.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Illumin360.Professionals.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Professionals service. Owns the <c>professionals</c> bounded-context database
/// (charter Part 13: database-per-service). The context owns and migration-manages all of its tables,
/// alongside the MassTransit transactional outbox.
/// </summary>
public sealed class ProfessionalsDbContext(DbContextOptions<ProfessionalsDbContext> options) : DbContext(options)
{
    /// <summary>The professional aggregate set.</summary>
    public DbSet<Professional> Professionals => Set<Professional>();

    /// <summary>Job-match set.</summary>
    public DbSet<ProfessionalMatch> Matches => Set<ProfessionalMatch>();

    /// <summary>Application pipeline-stage set.</summary>
    public DbSet<ProfessionalPipelineStage> Pipeline => Set<ProfessionalPipelineStage>();

    /// <summary>In-demand-roles set.</summary>
    public DbSet<ProfessionalSkillDemand> SkillDemand => Set<ProfessionalSkillDemand>();

    /// <summary>Skills set.</summary>
    public DbSet<ProfessionalSkill> Skills => Set<ProfessionalSkill>();

    /// <summary>Skill endorsements / references.</summary>
    public DbSet<SkillEndorsement> SkillEndorsements => Set<SkillEndorsement>();

    /// <summary>Activity-feed set.</summary>
    public DbSet<ProfessionalActivity> Activity => Set<ProfessionalActivity>();

    /// <summary>In-app notifications set.</summary>
    public DbSet<ProfessionalNotification> Notifications => Set<ProfessionalNotification>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("professionals");

        // MassTransit transactional outbox tables.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        var trendComparer = new ValueComparer<IReadOnlyList<int>>(
            (a, b) => a!.SequenceEqual(b!),
            v => v.Aggregate(0, HashCode.Combine),
            v => v.ToList());

        modelBuilder.Entity<Professional>(b =>
        {
            b.ToTable("professionals");
            b.HasKey(p => p.Id);
            b.Property(p => p.Id)
                .HasColumnName("id")
                .HasConversion(id => id.Value, value => new ProfessionalId(value));
            b.Property(p => p.FirstName).HasColumnName("first_name").HasMaxLength(100);
            b.Property(p => p.LastName).HasColumnName("last_name").HasMaxLength(100);
            b.Property(p => p.Role).HasColumnName("role").HasMaxLength(120);
            b.Property(p => p.City).HasColumnName("city").HasMaxLength(100);
            b.Property(p => p.Nationality).HasColumnName("nationality").HasMaxLength(100);
            b.Property(p => p.Availability).HasColumnName("availability").HasMaxLength(60);
            b.Property(p => p.Headline).HasColumnName("headline").HasMaxLength(200);
            b.Property(p => p.ProfileStrength).HasColumnName("profile_strength");
            b.Property(p => p.Percentile).HasColumnName("percentile");
            b.Property(p => p.MemberSince).HasColumnName("member_since").HasMaxLength(8);
            b.Property(p => p.ProfileViews).HasColumnName("profile_views");
            b.Property(p => p.ViewsDelta).HasColumnName("views_delta");
            b.Property(p => p.MatchOpportunities).HasColumnName("match_opportunities");
            b.Property(p => p.MatchDelta).HasColumnName("match_delta");
            b.Property(p => p.ActiveApplications).HasColumnName("active_applications");
            b.Property(p => p.ResponseRate).HasColumnName("response_rate");
            b.Property(p => p.AvgMatch).HasColumnName("avg_match");
            b.Property(p => p.Interviews).HasColumnName("interviews");
            b.Property(p => p.ViewsTrend)
                .HasColumnName("views_trend")
                .HasColumnType("integer[]")
                .HasConversion(v => v.ToArray(), v => v.ToList(), trendComparer);
            b.Property(p => p.SalaryRole).HasColumnName("salary_role").HasMaxLength(120);
            b.Property(p => p.SalaryP25).HasColumnName("salary_p25");
            b.Property(p => p.SalaryMedian).HasColumnName("salary_median");
            b.Property(p => p.SalaryP75).HasColumnName("salary_p75");
            b.Property(p => p.SalaryYou).HasColumnName("salary_you");
            b.Property(p => p.CreatedAt).HasColumnName("created_at");
            b.Property(p => p.CvObjectKey).HasColumnName("cv_object_key").HasMaxLength(400);
            b.Property(p => p.CvFileName).HasColumnName("cv_file_name").HasMaxLength(260);
            b.Property(p => p.CvContentType).HasColumnName("cv_content_type").HasMaxLength(120);
            b.Property(p => p.CvSize).HasColumnName("cv_size");
            b.Property(p => p.CvUploadedAt).HasColumnName("cv_uploaded_at");
            b.Ignore(p => p.DomainEvents);
            b.Ignore(p => p.FullName);
            b.Ignore(p => p.HasCv);
        });

        modelBuilder.Entity<ProfessionalMatch>(b =>
        {
            b.ToTable("professional_matches");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.ProfessionalId)
                .HasColumnName("professional_id")
                .HasConversion(id => id.Value, value => new ProfessionalId(value));
            b.Property(x => x.Role).HasColumnName("role").HasMaxLength(120);
            b.Property(x => x.Company).HasColumnName("company").HasMaxLength(120);
            b.Property(x => x.City).HasColumnName("city").HasMaxLength(100);
            b.Property(x => x.Industry).HasColumnName("industry").HasMaxLength(80);
            b.Property(x => x.MatchScore).HasColumnName("match_score");
            b.Property(x => x.SalaryLo).HasColumnName("salary_lo");
            b.Property(x => x.SalaryHi).HasColumnName("salary_hi");
            b.Property(x => x.Type).HasColumnName("type").HasMaxLength(40);
            b.Property(x => x.PostedLabel).HasColumnName("posted_label").HasMaxLength(20);
            b.Property(x => x.Sort).HasColumnName("sort");
            b.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasDefaultValue(MatchStatus.New)
                .HasConversion(new ValueConverter<MatchStatus, string>(
                    v => v == MatchStatus.Saved ? "saved" : v == MatchStatus.Dismissed ? "dismissed" : v == MatchStatus.Applied ? "applied" : "new",
                    v => v == "saved" ? MatchStatus.Saved : v == "dismissed" ? MatchStatus.Dismissed : v == "applied" ? MatchStatus.Applied : MatchStatus.New));
            b.HasIndex(x => x.ProfessionalId);
            b.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<ProfessionalPipelineStage>(b =>
        {
            b.ToTable("professional_pipeline");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.ProfessionalId)
                .HasColumnName("professional_id")
                .HasConversion(id => id.Value, value => new ProfessionalId(value));
            b.Property(x => x.Stage).HasColumnName("stage").HasMaxLength(40);
            b.Property(x => x.Value).HasColumnName("value");
            b.Property(x => x.Sort).HasColumnName("sort");
            b.HasIndex(x => x.ProfessionalId);
            b.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<ProfessionalSkillDemand>(b =>
        {
            b.ToTable("professional_skill_demand");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.ProfessionalId)
                .HasColumnName("professional_id")
                .HasConversion(id => id.Value, value => new ProfessionalId(value));
            b.Property(x => x.Role).HasColumnName("role").HasMaxLength(120);
            b.Property(x => x.Value).HasColumnName("value");
            b.Property(x => x.Sort).HasColumnName("sort");
            b.HasIndex(x => x.ProfessionalId);
            b.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<ProfessionalSkill>(b =>
        {
            b.ToTable("professional_skills");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.ProfessionalId)
                .HasColumnName("professional_id")
                .HasConversion(id => id.Value, value => new ProfessionalId(value));
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(80);
            b.Property(x => x.Level).HasColumnName("level");
            b.Property(x => x.Trend).HasColumnName("trend").HasMaxLength(20);
            b.Property(x => x.Sort).HasColumnName("sort");
            b.Property(x => x.Endorsements).HasColumnName("endorsements");
            b.HasIndex(x => x.ProfessionalId);
            b.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<SkillEndorsement>(b =>
        {
            b.ToTable("skill_endorsements");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.SkillId).HasColumnName("skill_id");
            b.Property(e => e.Endorser).HasColumnName("endorser").HasMaxLength(160);
            b.Property(e => e.Note).HasColumnName("note").HasMaxLength(500);
            b.Property(e => e.CreatedAt).HasColumnName("created_at");
            b.HasIndex(e => e.SkillId);
            b.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<ProfessionalNotification>(b =>
        {
            b.ToTable("professional_notifications");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.ProfessionalId)
                .HasColumnName("professional_id")
                .HasConversion(id => id.Value, value => new ProfessionalId(value));
            b.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(40);
            b.Property(x => x.Text).HasColumnName("text").HasMaxLength(300);
            b.Property(x => x.IsRead).HasColumnName("is_read");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.HasIndex(x => x.ProfessionalId);
            b.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<ProfessionalActivity>(b =>
        {
            b.ToTable("professional_activity");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.ProfessionalId)
                .HasColumnName("professional_id")
                .HasConversion(id => id.Value, value => new ProfessionalId(value));
            b.Property(x => x.Icon).HasColumnName("icon").HasMaxLength(30);
            b.Property(x => x.Text).HasColumnName("text").HasMaxLength(200);
            b.Property(x => x.WhenLabel).HasColumnName("when_label").HasMaxLength(40);
            b.Property(x => x.Sort).HasColumnName("sort");
            b.HasIndex(x => x.ProfessionalId);
            b.Ignore(x => x.DomainEvents);
        });
    }
}
