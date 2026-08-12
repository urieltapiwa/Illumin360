using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Illumin360.Payments.Infrastructure.Persistence;

/// <summary>Design-time factory so EF Core tooling can construct the context without booting the host.</summary>
public sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    /// <inheritdoc />
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql("Host=localhost;Database=illumin360_payments;Username=illumin;Password=design_time")
            .Options;

        return new PaymentsDbContext(options);
    }
}
