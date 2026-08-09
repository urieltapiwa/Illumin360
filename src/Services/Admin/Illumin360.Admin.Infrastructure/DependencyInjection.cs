using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Infrastructure.Messaging;
using Illumin360.Admin.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Admin.Infrastructure;

/// <summary>Wires Infrastructure adapters (EF Core, repositories, messaging) into DI.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Admin database context (PostgreSQL), repository adapters, the MassTransit bus
    /// outbox, and the data-store health check (tagged <c>ready</c>/<c>startup</c> — charter Part 11).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">App configuration (expects connection string "admin").</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddAdminInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("admin")
            ?? "Host=localhost;Port=5432;Database=illumin360_admin;Username=illumin;Password=illumin_dev_pw";

        services.AddDbContext<AdminDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IVerificationRepository, VerificationRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();

        var rabbitConnectionString = configuration.GetConnectionString("rabbitmq")
            ?? "amqp://illumin:illumin@localhost:5672";

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<AdminDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            x.SetKebabCaseEndpointNameFormatter();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri(rabbitConnectionString));
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "admin-db", tags: ["ready", "startup"]);

        return services;
    }
}
