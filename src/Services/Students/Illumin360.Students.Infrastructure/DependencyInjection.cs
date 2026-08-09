using Illumin360.Students.Application.Abstractions;
using Illumin360.Students.Infrastructure.Messaging;
using Illumin360.Students.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Students.Infrastructure;

/// <summary>Wires Infrastructure adapters (EF Core, repositories, messaging) into DI.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Students database context (PostgreSQL), repository adapters, the MassTransit
    /// bus outbox, and the data-store health check (tagged <c>ready</c> and <c>startup</c> — charter Part 11).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">App configuration (expects connection string "students").</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddStudentsInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("students")
            ?? "Host=localhost;Port=5432;Database=illumin360_students;Username=illumin;Password=illumin_dev_pw";

        services.AddDbContext<StudentsDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IStudentRepository, StudentRepository>();

        // --- Messaging: MassTransit + RabbitMQ with the EF Core transactional bus outbox ---
        // Integration events published by handlers are stored in the students database's outbox
        // tables in the same transaction as the aggregate, then delivered to RabbitMQ after commit.
        var rabbitConnectionString = configuration.GetConnectionString("rabbitmq")
            ?? "amqp://illumin:illumin@localhost:5672";

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<StudentsDbContext>(o =>
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
            .AddNpgSql(connectionString, name: "students-db", tags: ["ready", "startup"]);

        return services;
    }
}
