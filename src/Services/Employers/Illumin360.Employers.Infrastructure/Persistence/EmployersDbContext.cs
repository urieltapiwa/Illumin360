using Illumin360.Employers.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Employers.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Employers service. Owns the <c>employers</c> bounded-context database
/// (database-per-service — charter Part 13); all tables are migration-managed.
/// </summary>
public sealed class EmployersDbContext(DbContextOptions<EmployersDbContext> options) : DbContext(options)
{
    /// <summary>The employer aggregate set.</summary>
    public DbSet<Employer> Employers => Set<Employer>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("employers");

        modelBuilder.Entity<Employer>(b =>
        {
            b.ToTable("employers");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id)
                .HasColumnName("id")
                .HasConversion(id => id.Value, value => new EmployerId(value));
            b.Property(e => e.CompanyName).HasColumnName("company_name").HasMaxLength(160);
            b.Property(e => e.Industry).HasColumnName("industry").HasMaxLength(120);
            b.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
            b.Property(e => e.Website).HasColumnName("website").HasMaxLength(200);
            b.Property(e => e.About).HasColumnName("about").HasMaxLength(1000);
            b.Property(e => e.CreatedAt).HasColumnName("created_at");
            b.Ignore(e => e.DomainEvents);
        });
    }
}
