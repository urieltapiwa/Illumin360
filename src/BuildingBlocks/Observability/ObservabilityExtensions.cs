using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Builder;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Illumin360.Observability;

/// <summary>
/// One-call observability wiring for every Illumin360 service (charter Part 10.2).
/// Configures OpenTelemetry traces/metrics/logs (OTLP) and structured Serilog logging
/// with trace/span correlation, exported to Grafana Alloy.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Adds the standard Illumin360 telemetry: resource attributes, traces (ASP.NET Core,
    /// HttpClient, runtime), metrics (+ a per-service business meter), and Serilog→OTel logs.
    /// </summary>
    /// <param name="builder">The host application builder (composition root).</param>
    /// <param name="serviceName">Logical service name, e.g. <c>candidates</c>.</param>
    /// <returns>The same builder for chaining.</returns>
    public static WebApplicationBuilder AddProjectObservability(this WebApplicationBuilder builder, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                           ?? "http://localhost:4317";
        var environment = builder.Environment.EnvironmentName;
        var serviceVersion = typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "0.1.0";

        var resource = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceNamespace: "illumin360", serviceVersion: serviceVersion)
            .AddAttributes(new KeyValuePair<string, object>[]
            {
                new("deployment.environment", environment),
            });

        // --- Serilog: structured JSON to console + OTLP, trace/span ids auto-attached ---
        builder.Host.UseSerilog((ctx, sp, cfg) => cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.OpenTelemetry(o =>
            {
                o.Endpoint = otlpEndpoint;
                o.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName,
                    ["service.namespace"] = "illumin360",
                    ["deployment.environment"] = environment,
                };
            }));

        // --- OpenTelemetry traces + metrics ---
        builder.Services.AddOpenTelemetry()
            .WithTracing(t => t
                .SetResourceBuilder(resource)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(serviceName)
                .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)))
            .WithMetrics(m => m
                .SetResourceBuilder(resource)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(serviceName)
                .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));

        // Register the per-service ActivitySource + Meter for custom business telemetry.
        builder.Services.AddSingleton(new ActivitySource(serviceName));
        builder.Services.AddSingleton(new Meter(serviceName, serviceVersion));

        return builder;
    }

    /// <summary>
    /// Maps the three standard probe endpoints (charter Part 11):
    /// <c>/health/live</c>, <c>/health/ready</c>, <c>/health/startup</c>.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same app for chaining.</returns>
    public static WebApplication MapProjectHealthChecks(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var writer = UIResponseWriter.WriteHealthCheckUIResponse;

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = static check => check.Tags.Contains("live"),
            ResponseWriter = writer,
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = static check => check.Tags.Contains("ready"),
            ResponseWriter = writer,
        });
        app.MapHealthChecks("/health/startup", new HealthCheckOptions
        {
            Predicate = static check => check.Tags.Contains("startup"),
            ResponseWriter = writer,
        });
        return app;
    }
}
