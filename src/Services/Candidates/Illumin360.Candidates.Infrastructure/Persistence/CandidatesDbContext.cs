using Illumin360.Candidates.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Candidates.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Candidates service. Owns the <c>candidates</c> bounded-context
/// database (charter Part 13: database-per-service).
/// </summary>
public sealed class CandidatesDbContext(DbContextOptions<CandidatesDbContext> options) : DbContext(options)
{
    /// <summary>The candidate aggregate set.</summary>
    public DbSet<Candidate> Candidates => Set<Candidate>();

    /// <summary>Recruiter talent pools (shortlists).</summary>
    public DbSet<TalentPool> TalentPools => Set<TalentPool>();

    /// <summary>Talent-pool memberships.</summary>
    public DbSet<TalentPoolMember> TalentPoolMembers => Set<TalentPoolMember>();

    /// <summary>Recruiter notes on candidates.</summary>
    public DbSet<CandidateNote> CandidateNotes => Set<CandidateNote>();

    /// <summary>Tags / labels on candidates.</summary>
    public DbSet<CandidateTag> CandidateTags => Set<CandidateTag>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("candidates");

        // MassTransit transactional outbox tables (inbox/outbox state + outbox messages).
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<Candidate>(b =>
        {
            b.ToTable("candidates");
            b.HasKey(c => c.Id);
            b.Property(c => c.Id)
                .HasColumnName("id")
                .HasConversion(id => id.Value, value => new CandidateId(value));
            b.Property(c => c.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            b.Property(c => c.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            b.Property(c => c.City).HasColumnName("city").HasMaxLength(100).IsRequired();
            b.Property(c => c.Nationality).HasColumnName("nationality").HasMaxLength(100).IsRequired();
            b.Property(c => c.Availability).HasColumnName("availability_status").HasConversion<string>().HasMaxLength(30);
            b.Property(c => c.PublicHeadline).HasColumnName("public_headline").HasMaxLength(150);
            b.Property(c => c.CreatedAt).HasColumnName("created_at");
            b.Property(c => c.CvObjectKey).HasColumnName("cv_object_key").HasMaxLength(400);
            b.Property(c => c.CvFileName).HasColumnName("cv_file_name").HasMaxLength(260);
            b.Property(c => c.CvContentType).HasColumnName("cv_content_type").HasMaxLength(120);
            b.Property(c => c.CvSize).HasColumnName("cv_size");
            b.Property(c => c.CvUploadedAt).HasColumnName("cv_uploaded_at");
            b.Ignore(c => c.HasCv);
            b.Ignore(c => c.DomainEvents);
            b.HasIndex(c => c.City);
        });

        modelBuilder.Entity<TalentPool>(b =>
        {
            b.ToTable("talent_pools");
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).HasColumnName("id").HasConversion(id => id.Value, value => new TalentPoolId(value));
            b.Property(p => p.Name).HasColumnName("name").HasMaxLength(120);
            b.Property(p => p.CreatedAt).HasColumnName("created_at");
            b.Ignore(p => p.DomainEvents);
        });

        modelBuilder.Entity<CandidateNote>(b =>
        {
            b.ToTable("candidate_notes");
            b.HasKey(n => n.Id);
            b.Property(n => n.Id).HasColumnName("id");
            b.Property(n => n.CandidateId).HasColumnName("candidate_id").HasConversion(id => id.Value, value => new CandidateId(value));
            b.Property(n => n.Author).HasColumnName("author").HasMaxLength(160);
            b.Property(n => n.Body).HasColumnName("body").HasMaxLength(2000);
            b.Property(n => n.CreatedAt).HasColumnName("created_at");
            b.HasIndex(n => n.CandidateId);
            b.Ignore(n => n.DomainEvents);
        });

        modelBuilder.Entity<CandidateTag>(b =>
        {
            b.ToTable("candidate_tags");
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).HasColumnName("id");
            b.Property(t => t.CandidateId).HasColumnName("candidate_id").HasConversion(id => id.Value, value => new CandidateId(value));
            b.Property(t => t.Label).HasColumnName("label").HasMaxLength(40);
            b.Property(t => t.CreatedAt).HasColumnName("created_at");
            b.HasIndex(t => new { t.CandidateId, t.Label }).IsUnique();
            b.Ignore(t => t.DomainEvents);
        });

        modelBuilder.Entity<TalentPoolMember>(b =>
        {
            b.ToTable("talent_pool_members");
            b.HasKey(m => m.Id);
            b.Property(m => m.Id).HasColumnName("id");
            b.Property(m => m.PoolId).HasColumnName("pool_id").HasConversion(id => id.Value, value => new TalentPoolId(value));
            b.Property(m => m.CandidateId).HasColumnName("candidate_id").HasConversion(id => id.Value, value => new CandidateId(value));
            b.Property(m => m.AddedAt).HasColumnName("added_at");
            b.HasIndex(m => m.PoolId);
            b.HasIndex(m => new { m.PoolId, m.CandidateId }).IsUnique();
            b.Ignore(m => m.DomainEvents);
        });
    }
}
