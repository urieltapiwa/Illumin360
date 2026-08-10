using Illumin360.Email;
using Illumin360.Notifications.Worker.Consumers;
using Illumin360.Observability;
using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// --- Cross-cutting: OpenTelemetry + Serilog (charter Part 10) ---
builder.AddProjectObservability("notifications");

// --- Liveness probe (process up, no dependency checks) ---
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

// --- Messaging: MassTransit + RabbitMQ, consuming Candidates integration events ---
var rabbitConnectionString = builder.Configuration.GetConnectionString("rabbitmq")
    ?? "amqp://illumin:illumin@localhost:5672";

// --- Transactional email (SMTP; Mailpit in dev) ---
builder.Services.AddIllumin360Email(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CandidateRegisteredConsumer>();
    x.AddConsumer<ApplicationSubmittedConsumer>();
    x.AddConsumer<ApplicationStatusChangedConsumer>();
    x.AddConsumer<JobAlertDigestConsumer>();
    x.SetKebabCaseEndpointNameFormatter();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(rabbitConnectionString));
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// --- Health probes: /health/live, /health/ready, /health/startup (charter Part 11) ---
app.MapProjectHealthChecks();

app.Run();
