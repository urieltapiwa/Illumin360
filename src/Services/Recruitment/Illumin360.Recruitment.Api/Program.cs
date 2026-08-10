using Illumin360.Observability;
using Illumin360.Recruitment.Application;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Infrastructure;
using Illumin360.Recruitment.Infrastructure.Persistence;
using Illumin360.Security;
using Illumin360.Web;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Cross-cutting: OpenTelemetry + Serilog (charter Part 10) ---
builder.AddProjectObservability("recruitment");

// --- Liveness probe (process up, no dependency checks) ---
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"]);

// --- Clean Architecture layers ---
builder.Services.AddRecruitmentApplication();
builder.Services.AddRecruitmentInfrastructure(builder.Configuration);

// --- AuthN/AuthZ: validate Keycloak JWTs relayed by the BFF; expose admin role policies (charter Part 7) ---
builder.Services.AddIllumin360Auth(builder.Configuration);

// --- OpenAPI 3.1 (charter Part 7) ---
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply EF Core migrations on startup. Only the MassTransit outbox tables are created here — the
// recruitment_requests/applications tables are pre-existing (externally seeded) and excluded from
// migrations, so this records the outbox schema in __EFMigrationsHistory without touching seeded data.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecruitmentDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi(); // /openapi/v1.json

// --- Health probes: /health/live, /health/ready, /health/startup (charter Part 11) ---
app.MapProjectHealthChecks();

// --- API v1 endpoints ---
var v1 = app.MapGroup("/v1/recruitment").WithTags("Recruitment");

v1.MapGet("/requests", async (
        string? city,
        string? status,
        int? page,
        int? pageSize,
        IQueryHandler<GetRecruitmentRequestsQuery, IReadOnlyList<RecruitmentRequestDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(
            new GetRecruitmentRequestsQuery(city, status, page ?? 1, pageSize ?? 20), ct);
        return result.ToHttpResult();
    })
    .WithName("ListRecruitmentRequests")
    .WithSummary("List recruitment requests with optional city/status filter and paging.")
    .Produces<IReadOnlyList<RecruitmentRequestDto>>(StatusCodes.Status200OK);

v1.MapGet("/stats", async (
        IQueryHandler<GetRecruitmentStatsQuery, RecruitmentStatsDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetRecruitmentStatsQuery(), ct);
        return result.ToHttpResult();
    })
    .WithName("GetRecruitmentStats")
    .WithSummary("Aggregate recruitment statistics (funnel, hires trend, matching, top cities).")
    .Produces<RecruitmentStatsDto>(StatusCodes.Status200OK);

v1.MapGet("/requests/{id:guid}", async (
        Guid id,
        IQueryHandler<GetRecruitmentRequestByIdQuery, RecruitmentRequestDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetRecruitmentRequestByIdQuery(id), ct);
        return result.ToHttpResult();
    })
    .WithName("GetRecruitmentRequestById")
    .WithSummary("Fetch a single recruitment request by id.")
    .Produces<RecruitmentRequestDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapGet("/requests/{id:guid}/applications", async (
        Guid id,
        int? page,
        int? pageSize,
        IQueryHandler<GetApplicationsForRequestQuery, IReadOnlyList<ApplicationDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetApplicationsForRequestQuery(id, page ?? 1, pageSize ?? 50), ct);
        return result.ToHttpResult();
    })
    .WithName("ListApplicationsForRequest")
    .WithSummary("List applications for a recruitment request, highest match score first.")
    .Produces<IReadOnlyList<ApplicationDto>>(StatusCodes.Status200OK);

v1.MapPost("/requests", async (
        PostRecruitmentRequestCommand command,
        ICommandHandler<PostRecruitmentRequestCommand, RecruitmentRequestDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToCreatedResult(dto => $"/v1/recruitment/requests/{dto.Id}");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("PostRecruitmentRequest")
    .WithSummary("Post a new recruitment request. Requires an admin (write) role.")
    .Produces<RecruitmentRequestDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

v1.MapPost("/requests/{id:guid}/apply", async (
        Guid id,
        ApplyToRequestBody body,
        ICommandHandler<ApplyToRequestCommand, ApplicationDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(
            new ApplyToRequestCommand(id, body.TalentId, body.TalentType ?? "professional"), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.ProfessionalPolicy)
    .WithName("ApplyToRecruitmentRequest")
    .WithSummary("Apply to an open recruitment request. Requires a signed-in professional role.")
    .Produces<ApplicationDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapGet("/talents/{talentId:guid}/applications", async (
        Guid talentId,
        int? limit,
        IQueryHandler<GetTalentApplicationsQuery, IReadOnlyList<TalentApplicationDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetTalentApplicationsQuery(talentId, limit ?? 50), ct);
        return result.ToHttpResult();
    })
    .WithName("GetTalentApplications")
    .WithSummary("A talent's applications with role details and status, most recent first.")
    .Produces<IReadOnlyList<TalentApplicationDto>>(StatusCodes.Status200OK);

// --- Saved searches + job alerts (talent-owned) ---
v1.MapGet("/saved-searches", async (
        Guid talentId,
        IQueryHandler<GetSavedSearchesQuery, IReadOnlyList<SavedSearchDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetSavedSearchesQuery(talentId), ct);
        return result.ToHttpResult();
    })
    .WithName("GetSavedSearches")
    .WithSummary("List a talent's saved searches.")
    .Produces<IReadOnlyList<SavedSearchDto>>(StatusCodes.Status200OK);

v1.MapGet("/saved-searches/{id:guid}/results", async (
        Guid id,
        IQueryHandler<RunSavedSearchQuery, IReadOnlyList<RecruitmentRequestDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new RunSavedSearchQuery(id), ct);
        return result.ToHttpResult();
    })
    .WithName("RunSavedSearch")
    .WithSummary("Run a saved search — the open roles currently matching its criteria.")
    .Produces<IReadOnlyList<RecruitmentRequestDto>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/saved-searches", async (
        CreateSavedSearchBody body,
        ICommandHandler<CreateSavedSearchCommand, SavedSearchDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(
            new CreateSavedSearchCommand(body.TalentId, body.Label, body.City, body.Keyword, body.AlertsEnabled), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.ProfessionalPolicy)
    .WithName("CreateSavedSearch")
    .WithSummary("Save a role search. Requires a signed-in talent role.")
    .Produces<SavedSearchDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

v1.MapPost("/saved-searches/{id:guid}/alerts", async (
        Guid id,
        ToggleAlertsBody body,
        ICommandHandler<ToggleSavedSearchAlertsCommand, SavedSearchDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new ToggleSavedSearchAlertsCommand(id, body.Enabled), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.ProfessionalPolicy)
    .WithName("ToggleSavedSearchAlerts")
    .WithSummary("Enable/disable job alerts for a saved search. Requires a signed-in talent role.")
    .Produces<SavedSearchDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapDelete("/saved-searches/{id:guid}", async (
        Guid id,
        ICommandHandler<DeleteSavedSearchCommand, bool> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new DeleteSavedSearchCommand(id), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.ProfessionalPolicy)
    .WithName("DeleteSavedSearch")
    .WithSummary("Delete a saved search. Requires a signed-in talent role.")
    .Produces<bool>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

// --- Recruiter pipeline transitions on an application (admin/recruiter) ---
v1.MapPost("/applications/{id:guid}/advance", async (
        Guid id,
        ICommandHandler<AdvanceApplicationCommand, ApplicationDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new AdvanceApplicationCommand(id), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("AdvanceApplication")
    .WithSummary("Advance an application to the next stage (applied→reviewed→shortlisted→hired). Requires an admin (write) role.")
    .Produces<ApplicationDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/applications/{id:guid}/reject", async (
        Guid id,
        ICommandHandler<RejectApplicationCommand, ApplicationDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new RejectApplicationCommand(id), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RejectApplication")
    .WithSummary("Reject an application (terminal). Requires an admin (write) role.")
    .Produces<ApplicationDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

app.Run();

/// <summary>Request body for applying to a recruitment request.</summary>
/// <param name="TalentId">The applying talent's id.</param>
/// <param name="TalentType">Talent type (<c>student</c>/<c>professional</c>); defaults to professional.</param>
internal sealed record ApplyToRequestBody(Guid TalentId, string? TalentType);

/// <summary>Request body for creating a saved search.</summary>
/// <param name="TalentId">Owning talent id.</param>
/// <param name="Label">Label.</param>
/// <param name="City">Optional city filter.</param>
/// <param name="Keyword">Optional title keyword.</param>
/// <param name="AlertsEnabled">Whether alerts are enabled.</param>
internal sealed record CreateSavedSearchBody(Guid TalentId, string Label, string? City, string? Keyword, bool AlertsEnabled);

/// <summary>Request body for toggling job alerts.</summary>
/// <param name="Enabled">Whether alerts should be enabled.</param>
internal sealed record ToggleAlertsBody(bool Enabled);

/// <summary>Exposed so integration tests can use <c>WebApplicationFactory</c> (charter Part 14).</summary>
public partial class Program;
