using Illumin360.Billing.Application.Abstractions;
using Illumin360.Billing.Infrastructure.Persistence;
using Illumin360.Billing.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Billing.Infrastructure;

/// <summary>Wires Infrastructure adapters (EF Core, repository) into DI.</summary>
public static class DependencyInjection
{
    /// <summary>Registers the Billing database context, repository, and DB health check.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">App configuration (expects connection string "billing").</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddBillingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("billing")
            ?? "Host=localhost;Port=5432;Database=illumin360_billing;Username=illumin;Password=illumin_dev_pw";

        services.AddDbContext<BillingDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IBillingRepository, BillingRepository>();

        // The IBillingProvider is registered in the Api layer (where AddHttpClient is available): Fake by
        // default, a real recurring adapter only when Billing:Provider opts in. See Program.cs.
        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "billing-db", tags: ["ready", "startup"]);

        return services;
    }
}
