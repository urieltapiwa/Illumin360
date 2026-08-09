using System.Threading.RateLimiting;
using Illumin360.Observability;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Observability for the edge (charter Part 10).
builder.AddProjectObservability("gateway");

// YARP reverse proxy / ingress (charter Part 2).
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Coarse rate limiting at the edge (charter Part 7).
builder.Services.AddRateLimiter(o =>
{
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.User.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 100, Window = TimeSpan.FromMinutes(1) }));
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live", "ready"]);

var app = builder.Build();

app.UseRateLimiter();
app.MapProjectHealthChecks();
app.MapReverseProxy();

app.Run();
