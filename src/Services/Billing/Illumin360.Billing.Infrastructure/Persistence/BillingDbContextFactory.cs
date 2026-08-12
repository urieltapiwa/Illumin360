using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Illumin360.Billing.Infrastructure.Persistence;

/// <summary>Design-time factory so EF Core tooling can construct the context without booting the host.</summary>
public sealed class BillingDbContextFactory : IDesignTimeDbContextFactory<BillingDbContext>
{
    /// <inheritdoc />
    public BillingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseNpgsql("Host=localhost;Database=illumin360_billing;Username=illumin;Password=design_time")
            .Options;

        return new BillingDbContext(options);
    }
}
