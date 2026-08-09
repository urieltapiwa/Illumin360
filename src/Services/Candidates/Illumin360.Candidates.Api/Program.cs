using Illumin360.Candidates.Application;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Infrastructure;
using Illumin360.Candidates.Infrastructure.Persistence;
using Illumin360.Observability;
using Illumin360.Web;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Cross-cutting: OpenTelemetry + Serilog (charter Part 10) ---
builder.AddProjectObservability("candidates");

// --- Liveness probe (process up, no dependency checks) ---
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"]);

// --- Clean Architecture layers ---
builder.Services.AddCandidatesApplication();
builder.Services.AddCandidatesInfrastructure(builder.Configuration);

// --- OpenAPI 3.1 (charter Part 7) ---
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply EF Core migrations on startup: creates the candidate schema + MassTransit outbox tables and
// records them in __EFMigrationsHistory. Replaces the earlier EnsureCreated bootstrap (charter Part 13).
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CandidatesDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.MapOpenApi(); // /openapi/v1.json

// --- Health probes: /health/live, /health/ready, /health/startup (charter Part 11) ---
app.MapProjectHealthChecks();

// --- API v1 endpoints ---
var v1 = app.MapGroup("/v1/candidates").WithTags("Candidates");

v1.MapGet("/", async (
        string? city,
        int? page,
        int? pageSize,
        IQueryHandler<GetCandidatesQuery, IReadOnlyList<CandidateDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetCandidatesQuery(city, page ?? 1, pageSize ?? 20), ct);
        return result.ToHttpResult();
    })
    .WithName("ListCandidates")
    .WithSummary("List candidates with optional city filter and paging.")
    .Produces<IReadOnlyList<CandidateDto>>(StatusCodes.Status200OK);

v1.MapGet("/stats", async (
        IQueryHandler<GetCandidateStatsQuery, CandidateStatsDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetCandidateStatsQuery(), ct);
        return result.ToHttpResult();
    })
    .WithName("GetCandidateStats")
    .WithSummary("Aggregate candidate statistics (total + city/availability breakdowns).")
    .Produces<CandidateStatsDto>(StatusCodes.Status200OK);

v1.MapGet("/{id:guid}", async (
        Guid id,
        IQueryHandler<GetCandidateByIdQuery, CandidateDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetCandidateByIdQuery(id), ct);
        return result.ToHttpResult();
    })
    .WithName("GetCandidateById")
    .WithSummary("Fetch a single candidate by id.")
    .Produces<CandidateDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/", async (
        RegisterCandidateCommand command,
        ICommandHandler<RegisterCandidateCommand, CandidateDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToCreatedResult(dto => $"/v1/candidates/{dto.Id}");
    })
    .WithName("RegisterCandidate")
    .WithSummary("Register a new candidate into the talent pool.")
    .Produces<CandidateDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest);

app.Run();

/// <summary>Exposed so integration tests can use <c>WebApplicationFactory</c> (charter Part 14).</summary>
public partial class Program;
