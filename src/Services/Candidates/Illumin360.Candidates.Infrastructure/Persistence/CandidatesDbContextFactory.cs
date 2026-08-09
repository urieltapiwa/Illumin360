using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Illumin360.Candidates.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so EF Core tooling (<c>dotnet ef migrations</c>) can construct the context
/// without booting the web host. Uses a placeholder connection string — generating or scripting
/// migrations never opens a database connection, only the Npgsql provider's SQL dialect is needed.
/// </summary>
public sealed class CandidatesDbContextFactory : IDesignTimeDbContextFactory<CandidatesDbContext>
{
    /// <inheritdoc />
    public CandidatesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CandidatesDbContext>()
            .UseNpgsql("Host=localhost;Database=illumin360_candidates;Username=illumin;Password=design_time")
            .Options;

        return new CandidatesDbContext(options);
    }
}
