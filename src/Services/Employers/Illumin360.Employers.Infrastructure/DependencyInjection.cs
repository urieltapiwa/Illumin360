using Illumin360.Employers.Application.Abstractions;
using Illumin360.Employers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Employers.Infrastructure;

/// <summary>Wires Infrastructure adapters (EF Core, repositories) into DI.</summary>
public static class DependencyInjection
{
    /// <summary>Registers the Employers database context (PostgreSQL), repository, and DB health check.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">App configuration (expects connection string "employers").</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddEmployersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("employers")
            ?? "Host=localhost;Port=5432;Database=illumin360_employers;Username=illumin;Password=illumin_dev_pw";

        services.AddDbContext<EmployersDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IEmployerRepository, EmployerRepository>();

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "employers-db", tags: ["ready", "startup"]);

        return services;
    }
}
