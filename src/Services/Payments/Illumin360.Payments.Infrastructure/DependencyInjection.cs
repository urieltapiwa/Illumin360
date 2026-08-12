using Illumin360.Payments.Application.Abstractions;
using Illumin360.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Payments.Infrastructure;

/// <summary>Wires Infrastructure adapters (EF Core, repository, payment provider) into DI.</summary>
public static class DependencyInjection
{
    /// <summary>Registers the Payments database context, repository, payment provider, and DB health check.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">App configuration (expects connection string "payments").</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("payments")
            ?? "Host=localhost;Port=5432;Database=illumin360_payments;Username=illumin;Password=illumin_dev_pw";

        services.AddDbContext<PaymentsDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IPaymentsRepository, PaymentsRepository>();

        // The IPaymentProvider is registered in the Api layer (where AddHttpClient is available): Fake by
        // default, a real PSP adapter only when Payments:Provider opts in (decision D1). See Program.cs.
        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "payments-db", tags: ["ready", "startup"]);

        return services;
    }
}
