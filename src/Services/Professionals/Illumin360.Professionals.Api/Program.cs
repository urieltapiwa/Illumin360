using Illumin360.Observability;
using Illumin360.Security;
using Illumin360.Professionals.Application;
using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Application.Professionals;
using Illumin360.Professionals.Infrastructure;
using Illumin360.Professionals.Infrastructure.Persistence;
using Illumin360.Web;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Cross-cutting: OpenTelemetry + Serilog (charter Part 10) ---
builder.AddProjectObservability("professionals");

// --- Liveness probe (process up, no dependency checks) ---
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"]);

// --- Clean Architecture layers ---
builder.Services.AddProfessionalsApplication();
builder.Services.AddProfessionalsInfrastructure(builder.Configuration);

// --- AuthN/AuthZ: validate Keycloak JWTs relayed by the BFF; expose admin role policies (charter Part 7) ---
builder.Services.AddIllumin360Auth(builder.Configuration);

// --- OpenAPI 3.1 (charter Part 7) ---
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply EF Core migrations on startup, then seed the demo cohort if the database is empty.
// The Professionals context owns all its tables (migration-managed), unlike Recruitment/Candidates.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProfessionalsDbContext>();
    await db.Database.MigrateAsync();
    await ProfessionalsSeeder.SeedAsync(db, CancellationToken.None);
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi(); // /openapi/v1.json

// --- Health probes: /health/live, /health/ready, /health/startup (charter Part 11) ---
app.MapProjectHealthChecks();

// --- API v1 endpoints ---
var v1 = app.MapGroup("/v1/professionals").WithTags("Professionals");

v1.MapGet("/me", async (
        IQueryHandler<GetProfessionalDashboardQuery, ProfessionalDashboardDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetProfessionalDashboardQuery(), ct);
        return result.ToHttpResult();
    })
    .WithName("GetMyProfessionalDashboard")
    .WithSummary("Dashboard for the current (demo) professional.")
    .Produces<ProfessionalDashboardDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapGet("/{id:guid}", async (
        Guid id,
        IQueryHandler<GetProfessionalDashboardQuery, ProfessionalDashboardDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetProfessionalDashboardQuery(id), ct);
        return result.ToHttpResult();
    })
    .WithName("GetProfessionalDashboardById")
    .WithSummary("Dashboard for a professional by id.")
    .Produces<ProfessionalDashboardDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/", async (
        RegisterProfessionalCommand command,
        ICommandHandler<RegisterProfessionalCommand, ProfessionalSummaryDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToCreatedResult(dto => $"/v1/professionals/{dto.Id}");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RegisterProfessional")
    .WithSummary("Register a new professional on the marketplace. Requires an admin (write) role.")
    .Produces<ProfessionalSummaryDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

// --- Professional self-service actions on the current ("me") profile (role: professional) ---
static MatchAction? ParseMatchAction(string a) => a switch
{
    "save" => MatchAction.Save,
    "dismiss" => MatchAction.Dismiss,
    "apply" => MatchAction.Apply,
    _ => null,
};

v1.MapPost("/me/matches/{id:guid}/{action}", async (
        Guid id,
        string action,
        ICommandHandler<UpdateMatchStatusCommand, MatchDto> handler,
        CancellationToken ct) =>
    {
        if (ParseMatchAction(action) is not { } parsed)
        {
            return Results.NotFound();
        }

        var result = await handler.HandleAsync(new UpdateMatchStatusCommand(id, parsed), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.ProfessionalPolicy)
    .WithName("UpdateMatchStatus")
    .WithSummary("Save / dismiss / apply to a match on the current profile. Requires a professional role.")
    .Produces<MatchDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/me/availability", async (
        SetAvailabilityCommand command,
        ICommandHandler<SetAvailabilityCommand, string> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.ProfessionalPolicy)
    .WithName("SetAvailability")
    .WithSummary("Update the current profile's availability. Requires a professional role.")
    .Produces<string>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

// --- CV upload / download for the current ("me") profile ---
v1.MapPost("/me/cv", async (
        IFormFile file,
        ICommandHandler<UploadCvCommand, CvDto> handler,
        CancellationToken ct) =>
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest();
        }

        await using var stream = file.OpenReadStream();
        var result = await handler.HandleAsync(
            new UploadCvCommand(file.FileName, file.ContentType, file.Length, stream), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.ProfessionalPolicy)
    .DisableAntiforgery()
    .WithName("UploadCv")
    .WithSummary("Upload or replace the current profile's CV (PDF/DOC/DOCX, ≤5MB). Requires a professional role.")
    .Produces<CvDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

v1.MapGet("/me/cv", async (
        IQueryHandler<GetCvMetadataQuery, CvDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetCvMetadataQuery(), ct);
        return result.ToHttpResult();
    })
    .WithName("GetCvMetadata")
    .WithSummary("Metadata for the current profile's CV, if uploaded.")
    .Produces<CvDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapGet("/me/cv/download", async (
        IQueryHandler<DownloadCvQuery, CvContent> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new DownloadCvQuery(), ct);
        return result.IsSuccess
            ? Results.File(result.Value!.Content, result.Value!.ContentType, result.Value!.FileName)
            : result.ToHttpResult();
    })
    .WithName("DownloadCv")
    .WithSummary("Download the current profile's CV, if uploaded.")
    .Produces(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

// --- Rank marketplace roles for the current ("me") professional via the shared match engine ---
v1.MapPost("/me/role-scores", async (
        List<RoleToScore> roles,
        IQueryHandler<ScoreRolesQuery, IReadOnlyList<RoleScoreDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new ScoreRolesQuery(roles ?? []), ct);
        return result.ToHttpResult();
    })
    .WithName("ScoreOpenRoles")
    .WithSummary("Score a set of marketplace roles against the current profile (recommendations).")
    .Produces<IReadOnlyList<RoleScoreDto>>(StatusCodes.Status200OK);

v1.MapPost("/me/cv/parse", async (
        IQueryHandler<ParseCvSkillsQuery, CvSkillsDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new ParseCvSkillsQuery(), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.ProfessionalPolicy)
    .WithName("ParseCvSkills")
    .WithSummary("Extract skills from the current profile's uploaded CV. Requires a professional role.")
    .Produces<CvSkillsDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/me/cv/apply-skills", async (
        ICommandHandler<ApplyCvSkillsCommand, AppliedSkillsDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new ApplyCvSkillsCommand(), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.ProfessionalPolicy)
    .WithName("ApplyCvSkills")
    .WithSummary("Parse the current profile's CV and add newly detected skills to the profile. Requires a professional role.")
    .Produces<AppliedSkillsDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

// --- In-app notification center for the current ("me") profile ---
v1.MapGet("/me/notifications", async (
        bool? unreadOnly,
        IQueryHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetNotificationsQuery(unreadOnly ?? false), ct);
        return result.ToHttpResult();
    })
    .WithName("GetNotifications")
    .WithSummary("List the current profile's in-app notifications.")
    .Produces<IReadOnlyList<NotificationDto>>(StatusCodes.Status200OK);

v1.MapPost("/me/notifications/{id:guid}/read", async (
        Guid id,
        ICommandHandler<MarkNotificationReadCommand, bool> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new MarkNotificationReadCommand(id), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.ProfessionalPolicy)
    .WithName("MarkNotificationRead")
    .WithSummary("Mark a notification read. Requires a professional role.")
    .Produces<bool>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/me/notifications/read-all", async (
        ICommandHandler<MarkAllNotificationsReadCommand, int> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new MarkAllNotificationsReadCommand(), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.ProfessionalPolicy)
    .WithName("MarkAllNotificationsRead")
    .WithSummary("Mark all notifications read. Requires a professional role.")
    .Produces<int>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

app.Run();

/// <summary>Exposed so integration tests can use <c>WebApplicationFactory</c> (charter Part 14).</summary>
public partial class Program;
