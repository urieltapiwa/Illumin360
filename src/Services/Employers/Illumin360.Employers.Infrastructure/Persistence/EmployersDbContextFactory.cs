using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Illumin360.Employers.Infrastructure.Persistence;

/// <summary>Design-time factory so EF Core tooling can construct the context without booting the host.</summary>
public sealed class EmployersDbContextFactory : IDesignTimeDbContextFactory<EmployersDbContext>
{
    /// <inheritdoc />
    public EmployersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<EmployersDbContext>()
            .UseNpgsql("Host=localhost;Database=illumin360_employers;Username=illumin;Password=design_time")
            .Options;

        return new EmployersDbContext(options);
    }
}
