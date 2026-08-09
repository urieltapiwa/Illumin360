using System.Security.Claims;
using Illumin360.Admin.Application;
using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Application.Verifications;
using Illumin360.Admin.Infrastructure;
using Illumin360.Admin.Infrastructure.Persistence;
using Illumin360.Observability;
using Illumin360.Security;
using Illumin360.Web;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Cross-cutting: OpenTelemetry + Serilog (charter Part 10) ---
builder.AddProjectObservability("admin");

// --- Liveness probe (process up, no dependency checks) ---
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"]);

// --- Clean Architecture layers ---
builder.Services.AddAdminApplication();
builder.Services.AddAdminInfrastructure(builder.Configuration);

// --- AuthN/AuthZ: validate Keycloak JWTs relayed by the BFF; expose admin role policies (charter Part 7) ---
builder.Services.AddIllumin360Auth(builder.Configuration);

// --- OpenAPI 3.1 (charter Part 7) ---
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply EF Core migrations on startup, then seed the demo verification queue if empty.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    await db.Database.MigrateAsync();
    await AdminSeeder.SeedAsync(db, CancellationToken.None);
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi(); // /openapi/v1.json

// --- Health probes: /health/live, /health/ready, /health/startup (charter Part 11) ---
app.MapProjectHealthChecks();

// --- API v1 endpoints (all admin-tier; charter Part 7 — admin.read to view, admin.write to act) ---
var v1 = app.MapGroup("/v1/admin").WithTags("Admin");

v1.MapGet("/verifications", async (
        string? status,
        IQueryHandler<GetVerificationsQuery, IReadOnlyList<VerificationDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetVerificationsQuery(status ?? "pending"), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminPolicy)
    .WithName("ListVerifications")
    .WithSummary("List verifications (default: pending). Requires an admin role.")
    .Produces<IReadOnlyList<VerificationDto>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

v1.MapPost("/verifications/{id:guid}/approve", async (
        Guid id,
        ClaimsPrincipal user,
        ICommandHandler<DecideVerificationCommand, VerificationDto> handler,
        CancellationToken ct) =>
    {
        var command = new DecideVerificationCommand(id, VerificationDecision.Approve, user.Identity?.Name ?? "admin");
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("ApproveVerification")
    .WithSummary("Approve a pending verification. Requires an admin (write) role.")
    .Produces<VerificationDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/verifications/{id:guid}/reject", async (
        Guid id,
        ClaimsPrincipal user,
        ICommandHandler<DecideVerificationCommand, VerificationDto> handler,
        CancellationToken ct) =>
    {
        var command = new DecideVerificationCommand(id, VerificationDecision.Reject, user.Identity?.Name ?? "admin");
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RejectVerification")
    .WithSummary("Reject a pending verification. Requires an admin (write) role.")
    .Produces<VerificationDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

app.Run();

/// <summary>Exposed so integration tests can use <c>WebApplicationFactory</c> (charter Part 14).</summary>
public partial class Program;
