using System.Security.Claims;
using Illumin360.Admin.Application;
using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Application.Accounts;
using Illumin360.Admin.Application.Audit;
using Illumin360.Admin.Application.Dashboard;
using Illumin360.Admin.Application.Tickets;
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

// --- Dashboard summaries (read-only aggregations over the admin data; anonymous like the other
// portal dashboards — mutations below still require the admin role). ---
v1.MapGet("/summary", async (
        IQueryHandler<GetAdminSummaryQuery, AdminSummaryDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetAdminSummaryQuery(), ct);
        return result.ToHttpResult();
    })
    .WithName("GetAdminSummary")
    .WithSummary("Platform-operations summary for the Admin dashboard (accounts, verifications, tickets).")
    .Produces<AdminSummaryDto>(StatusCodes.Status200OK);

v1.MapGet("/talent-insights", async (
        IQueryHandler<GetTalentInsightsQuery, TalentInsightsDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetTalentInsightsQuery(), ct);
        return result.ToHttpResult();
    })
    .WithName("GetTalentInsights")
    .WithSummary("Talent-marketplace insights for the Business dashboard (talent, companies, verification).")
    .Produces<TalentInsightsDto>(StatusCodes.Status200OK);

v1.MapGet("/support-summary", async (
        IQueryHandler<GetSupportSummaryQuery, SupportSummaryDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetSupportSummaryQuery(), ct);
        return result.ToHttpResult();
    })
    .WithName("GetSupportSummary")
    .WithSummary("Support-queue metrics for the Support dashboard (open/assigned/resolved, by priority).")
    .Produces<SupportSummaryDto>(StatusCodes.Status200OK);

v1.MapGet("/audit", async (
        string? action,
        int? page,
        int? pageSize,
        IQueryHandler<GetAuditLogQuery, IReadOnlyList<AuditEntryDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetAuditLogQuery(action, page ?? 1, pageSize ?? 50), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminPolicy)
    .WithName("GetAuditLog")
    .WithSummary("View the administrative audit trail (newest first, optional action filter). Requires an admin role.")
    .Produces<IReadOnlyList<AuditEntryDto>>(StatusCodes.Status200OK);

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

// --- Support tickets (Phase 2) ---
v1.MapGet("/tickets", async (
        string? status,
        IQueryHandler<GetTicketsQuery, IReadOnlyList<TicketDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetTicketsQuery(status ?? "open"), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.SupportPolicy)
    .WithName("ListTickets")
    .WithSummary("List support tickets (default: open). Requires an admin role.")
    .Produces<IReadOnlyList<TicketDto>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

v1.MapPost("/tickets/{id:guid}/assign", async (
        Guid id,
        ClaimsPrincipal user,
        ICommandHandler<TriageTicketCommand, TicketDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(
            new TriageTicketCommand(id, TicketAction.Assign, user.Identity?.Name ?? "admin"), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.SupportPolicy)
    .WithName("AssignTicket")
    .WithSummary("Assign a ticket to the acting admin. Requires an admin (write) role.")
    .Produces<TicketDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/tickets/{id:guid}/resolve", async (
        Guid id,
        ClaimsPrincipal user,
        ICommandHandler<TriageTicketCommand, TicketDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(
            new TriageTicketCommand(id, TicketAction.Resolve, user.Identity?.Name ?? "admin"), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.SupportPolicy)
    .WithName("ResolveTicket")
    .WithSummary("Resolve a ticket. Requires an admin (write) role.")
    .Produces<TicketDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

// --- User accounts (Phase 3) ---
v1.MapGet("/accounts", async (
        string? status,
        IQueryHandler<GetAccountsQuery, IReadOnlyList<AccountDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetAccountsQuery(status), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminPolicy)
    .WithName("ListAccounts")
    .WithSummary("List platform accounts (optionally by status). Requires an admin role.")
    .Produces<IReadOnlyList<AccountDto>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

v1.MapPost("/accounts/{id:guid}/suspend", async (
        Guid id,
        ClaimsPrincipal user,
        ICommandHandler<SetAccountStatusCommand, AccountDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(
            new SetAccountStatusCommand(id, AccountAction.Suspend, user.Identity?.Name ?? "admin"), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("SuspendAccount")
    .WithSummary("Suspend a platform account. Requires an admin (write) role.")
    .Produces<AccountDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/accounts/{id:guid}/activate", async (
        Guid id,
        ClaimsPrincipal user,
        ICommandHandler<SetAccountStatusCommand, AccountDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(
            new SetAccountStatusCommand(id, AccountAction.Activate, user.Identity?.Name ?? "admin"), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("ActivateAccount")
    .WithSummary("Reactivate a suspended account. Requires an admin (write) role.")
    .Produces<AccountDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

app.Run();

/// <summary>Exposed so integration tests can use <c>WebApplicationFactory</c> (charter Part 14).</summary>
public partial class Program;
