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
    }
}
