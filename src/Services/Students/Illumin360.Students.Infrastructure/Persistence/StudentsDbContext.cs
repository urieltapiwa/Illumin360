using Illumin360.Students.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Illumin360.Students.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Students service. Owns the <c>students</c> bounded-context database
/// (charter Part 13: database-per-service). Unlike Recruitment/Candidates (which map onto
/// externally-seeded tables), the Students context owns and migration-manages all of its tables,
/// alongside the MassTransit transactional outbox.
/// </summary>
public sealed class StudentsDbContext(DbContextOptions<StudentsDbContext> options) : DbContext(options)
{
    /// <summary>The student aggregate set.</summary>
    public DbSet<Student> Students => Set<Student>();

    /// <summary>Student skills set.</summary>
    public DbSet<StudentSkill> Skills => Set<StudentSkill>();

    /// <summary>Student learning-module set.</summary>
    public DbSet<StudentLearning> Learning => Set<StudentLearning>();

    /// <summary>Student match set.</summary>
    public DbSet<StudentMatch> Matches => Set<StudentMatch>();

    /// <summary>Student pipeline-stage set.</summary>
    public DbSet<StudentPipelineStage> Pipeline => Set<StudentPipelineStage>();

    /// <summary>Student activity-feed set.</summary>
    public DbSet<StudentActivity> Activity => Set<StudentActivity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("students");

        // MassTransit transactional outbox tables.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        var trendComparer = new ValueComparer<IReadOnlyList<int>>(
            (a, b) => a!.SequenceEqual(b!),
            v => v.Aggregate(0, HashCode.Combine),
            v => v.ToList());

        modelBuilder.Entity<Student>(b =>
        {
            b.ToTable("students");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id)
                .HasColumnName("id")
                .HasConversion(id => id.Value, value => new StudentId(value));
            b.Property(s => s.FirstName).HasColumnName("first_name").HasMaxLength(100);
            b.Property(s => s.LastName).HasColumnName("last_name").HasMaxLength(100);
            b.Property(s => s.Field).HasColumnName("field").HasMaxLength(120);
            b.Property(s => s.School).HasColumnName("school").HasMaxLength(160);
            b.Property(s => s.Year).HasColumnName("year").HasMaxLength(40);
            b.Property(s => s.Graduating).HasColumnName("graduating").HasMaxLength(8);
            b.Property(s => s.Program).HasColumnName("program").HasMaxLength(120);
            b.Property(s => s.City).HasColumnName("city").HasMaxLength(100);
            b.Property(s => s.Availability).HasColumnName("availability").HasMaxLength(60).HasDefaultValue("Open to internships");
            b.Property(s => s.Readiness).HasColumnName("readiness");
            b.Property(s => s.ProfileViews).HasColumnName("profile_views");
            b.Property(s => s.ViewsDelta).HasColumnName("views_delta");
            b.Property(s => s.MentorSessions).HasColumnName("mentor_sessions");
            b.Property(s => s.ApplicationsCount).HasColumnName("applications_count");
            b.Property(s => s.ViewsTrend)
                .HasColumnName("views_trend")
                .HasColumnType("integer[]")
                .HasConversion(v => v.ToArray(), v => v.ToList(), trendComparer);
            b.Property(s => s.CreatedAt).HasColumnName("created_at");
            b.Ignore(s => s.DomainEvents);
            b.Ignore(s => s.FullName);
        });

        modelBuilder.Entity<StudentSkill>(b =>
        {
            b.ToTable("student_skills");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.StudentId)
                .HasColumnName("student_id")
                .HasConversion(id => id.Value, value => new StudentId(value));
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(80);
            b.Property(x => x.Level).HasColumnName("level");
            b.Property(x => x.Sort).HasColumnName("sort");
            b.HasIndex(x => x.StudentId);
            b.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<StudentLearning>(b =>
        {
            b.ToTable("student_learning");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.StudentId)
                .HasColumnName("student_id")
                .HasConversion(id => id.Value, value => new StudentId(value));
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(120);
            b.Property(x => x.Progress).HasColumnName("progress");
            b.Property(x => x.Tag).HasColumnName("tag").HasMaxLength(20);
            b.Property(x => x.Sort).HasColumnName("sort");
            b.HasIndex(x => x.StudentId);
            b.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<StudentMatch>(b =>
        {
            b.ToTable("student_matches");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.StudentId)
                .HasColumnName("student_id")
                .HasConversion(id => id.Value, value => new StudentId(value));
            b.Property(x => x.Role).HasColumnName("role").HasMaxLength(120);
            b.Property(x => x.Company).HasColumnName("company").HasMaxLength(120);
            b.Property(x => x.City).HasColumnName("city").HasMaxLength(100);
            b.Property(x => x.MatchScore).HasColumnName("match_score");
            b.Property(x => x.StipendLo).HasColumnName("stipend_lo");
            b.Property(x => x.StipendHi).HasColumnName("stipend_hi");
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
            b.HasIndex(x => x.StudentId);
            b.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<StudentPipelineStage>(b =>
        {
            b.ToTable("student_pipeline");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.StudentId)
                .HasColumnName("student_id")
                .HasConversion(id => id.Value, value => new StudentId(value));
            b.Property(x => x.Stage).HasColumnName("stage").HasMaxLength(40);
            b.Property(x => x.Value).HasColumnName("value");
            b.Property(x => x.Sort).HasColumnName("sort");
            b.HasIndex(x => x.StudentId);
            b.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<StudentActivity>(b =>
        {
            b.ToTable("student_activity");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.StudentId)
                .HasColumnName("student_id")
                .HasConversion(id => id.Value, value => new StudentId(value));
            b.Property(x => x.Text).HasColumnName("text").HasMaxLength(200);
            b.Property(x => x.WhenLabel).HasColumnName("when_label").HasMaxLength(40);
            b.Property(x => x.Sort).HasColumnName("sort");
            b.HasIndex(x => x.StudentId);
            b.Ignore(x => x.DomainEvents);
        });
    }
}
