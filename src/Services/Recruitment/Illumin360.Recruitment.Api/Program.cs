using Illumin360.Observability;
using Illumin360.Recruitment.Application;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Infrastructure;
using Illumin360.Recruitment.Infrastructure.Persistence;
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
    .WithName("PostRecruitmentRequest")
    .WithSummary("Post a new recruitment request.")
    .Produces<RecruitmentRequestDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest);

app.Run();

/// <summary>Exposed so integration tests can use <c>WebApplicationFactory</c> (charter Part 14).</summary>
public partial class Program;
