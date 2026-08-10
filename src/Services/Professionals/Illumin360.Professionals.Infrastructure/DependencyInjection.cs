using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Infrastructure.Messaging;
using Illumin360.Professionals.Infrastructure.Persistence;
using Illumin360.Storage;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Professionals.Infrastructure;

/// <summary>Wires Infrastructure adapters (EF Core, repositories, messaging) into DI.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Professionals database context (PostgreSQL), repository adapters, the MassTransit
    /// bus outbox, and the data-store health check (tagged <c>ready</c> and <c>startup</c> — charter Part 11).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">App configuration (expects connection string "professionals").</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddProfessionalsInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("professionals")
            ?? "Host=localhost;Port=5432;Database=illumin360_professionals;Username=illumin;Password=illumin_dev_pw";

        services.AddDbContext<ProfessionalsDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IProfessionalRepository, ProfessionalRepository>();

        // Object storage (MinIO/S3) for CV uploads.
        services.AddIllumin360Storage(configuration);

        // --- Messaging: MassTransit + RabbitMQ with the EF Core transactional bus outbox ---
        // Integration events published by handlers are stored in the professionals database's outbox
        // tables in the same transaction as the aggregate, then delivered to RabbitMQ after commit.
        var rabbitConnectionString = configuration.GetConnectionString("rabbitmq")
            ?? "amqp://illumin:illumin@localhost:5672";

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<ProfessionalsDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            // Consume recruitment events to raise in-app notifications for the professional.
            x.AddConsumer<Messaging.ApplicationStatusNotificationConsumer>();
            x.AddConsumer<Messaging.JobAlertNotificationConsumer>();

            x.SetKebabCaseEndpointNameFormatter();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri(rabbitConnectionString));
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "professionals-db", tags: ["ready", "startup"]);

        return services;
    }
}
