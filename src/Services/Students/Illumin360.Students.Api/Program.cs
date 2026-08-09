using Illumin360.Observability;
using Illumin360.Security;
using Illumin360.Students.Application;
using Illumin360.Students.Application.Abstractions;
using Illumin360.Students.Application.Students;
using Illumin360.Students.Infrastructure;
using Illumin360.Students.Infrastructure.Persistence;
using Illumin360.Web;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Cross-cutting: OpenTelemetry + Serilog (charter Part 10) ---
builder.AddProjectObservability("students");

// --- Liveness probe (process up, no dependency checks) ---
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"]);

// --- Clean Architecture layers ---
builder.Services.AddStudentsApplication();
builder.Services.AddStudentsInfrastructure(builder.Configuration);

// --- AuthN/AuthZ: validate Keycloak JWTs relayed by the BFF; expose admin role policies (charter Part 7) ---
builder.Services.AddIllumin360Auth(builder.Configuration);

// --- OpenAPI 3.1 (charter Part 7) ---
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply EF Core migrations on startup, then seed the demo cohort if the database is empty.
// The Students context owns all its tables (migration-managed), unlike Recruitment/Candidates.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StudentsDbContext>();
    await db.Database.MigrateAsync();
    await StudentsSeeder.SeedAsync(db, CancellationToken.None);
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi(); // /openapi/v1.json

// --- Health probes: /health/live, /health/ready, /health/startup (charter Part 11) ---
app.MapProjectHealthChecks();

// --- API v1 endpoints ---
var v1 = app.MapGroup("/v1/students").WithTags("Students");

v1.MapGet("/me", async (
        IQueryHandler<GetStudentDashboardQuery, StudentDashboardDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetStudentDashboardQuery(), ct);
        return result.ToHttpResult();
    })
    .WithName("GetMyStudentDashboard")
    .WithSummary("Dashboard for the current (demo) student.")
    .Produces<StudentDashboardDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapGet("/{id:guid}", async (
        Guid id,
        IQueryHandler<GetStudentDashboardQuery, StudentDashboardDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetStudentDashboardQuery(id), ct);
        return result.ToHttpResult();
    })
    .WithName("GetStudentDashboardById")
    .WithSummary("Dashboard for a student by id.")
    .Produces<StudentDashboardDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/", async (
        RegisterStudentCommand command,
        ICommandHandler<RegisterStudentCommand, StudentSummaryDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToCreatedResult(dto => $"/v1/students/{dto.Id}");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RegisterStudent")
    .WithSummary("Register a new student on the programme. Requires an admin (write) role.")
    .Produces<StudentSummaryDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

app.Run();

/// <summary>Exposed so integration tests can use <c>WebApplicationFactory</c> (charter Part 14).</summary>
public partial class Program;
