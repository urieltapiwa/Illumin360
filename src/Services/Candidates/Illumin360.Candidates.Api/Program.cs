using Illumin360.Candidates.Application;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Infrastructure;
using Illumin360.Candidates.Infrastructure.Persistence;
using Illumin360.Observability;
using Illumin360.Security;
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

// --- AuthN/AuthZ: validate Keycloak JWTs relayed by the BFF; expose admin role policies (charter Part 7) ---
builder.Services.AddIllumin360Auth(builder.Configuration);

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
app.UseAuthentication();
app.UseAuthorization();
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

v1.MapGet("/search", async (
        string? city,
        string? availability,
        string? q,
        bool? hasCv,
        int? page,
        int? pageSize,
        IQueryHandler<SearchCandidatesQuery, CandidateSearchResultDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new SearchCandidatesQuery(city, availability, q, hasCv, page ?? 1, pageSize ?? 20), ct);
        return result.ToHttpResult();
    })
    .WithName("SearchCandidates")
    .WithSummary("Faceted candidate search over city, availability, keyword and CV presence — returns matches, total and facet counts.")
    .Produces<CandidateSearchResultDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest);

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

v1.MapGet("/top", async (
        string? title,
        string? city,
        int? limit,
        IQueryHandler<GetTopCandidatesQuery, IReadOnlyList<RankedCandidateDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetTopCandidatesQuery(title ?? string.Empty, city, limit ?? 10), ct);
        return result.ToHttpResult();
    })
    .WithName("GetTopCandidates")
    .WithSummary("Rank candidates against a role (employer 'top candidates for this role').")
    .Produces<IReadOnlyList<RankedCandidateDto>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest);

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
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RegisterCandidate")
    .WithSummary("Register a new candidate into the talent pool. Requires an admin (write) role.")
    .Produces<CandidateDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

// --- Per-candidate CV upload / download (recruiter/admin registry) ---
v1.MapPost("/{id:guid}/cv", async (
        Guid id,
        IFormFile file,
        ICommandHandler<UploadCandidateCvCommand, CvDto> handler,
        CancellationToken ct) =>
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest();
        }

        await using var stream = file.OpenReadStream();
        var result = await handler.HandleAsync(
            new UploadCandidateCvCommand(id, file.FileName, file.ContentType, file.Length, stream), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .DisableAntiforgery()
    .WithName("UploadCandidateCv")
    .WithSummary("Upload or replace a candidate's CV (PDF/DOC/DOCX, ≤5MB). Requires an admin (write) role.")
    .Produces<CvDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapGet("/{id:guid}/cv", async (
        Guid id,
        IQueryHandler<GetCandidateCvMetadataQuery, CvDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetCandidateCvMetadataQuery(id), ct);
        return result.ToHttpResult();
    })
    .WithName("GetCandidateCvMetadata")
    .WithSummary("Metadata for a candidate's CV, if uploaded.")
    .Produces<CvDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapGet("/{id:guid}/cv/download", async (
        Guid id,
        IQueryHandler<DownloadCandidateCvQuery, CvContent> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new DownloadCandidateCvQuery(id), ct);
        return result.IsSuccess
            ? Results.File(result.Value!.Content, result.Value!.ContentType, result.Value!.FileName)
            : result.ToHttpResult();
    })
    .WithName("DownloadCandidateCv")
    .WithSummary("Download a candidate's CV, if uploaded.")
    .Produces(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

// --- Talent pools / shortlists (recruiter) ---
v1.MapGet("/pools", async (
        IQueryHandler<GetPoolsQuery, IReadOnlyList<TalentPoolDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetPoolsQuery(), ct);
        return result.ToHttpResult();
    })
    .WithName("GetTalentPools")
    .WithSummary("List recruiter talent pools (shortlists).")
    .Produces<IReadOnlyList<TalentPoolDto>>(StatusCodes.Status200OK);

v1.MapGet("/pools/{id:guid}/members", async (
        Guid id,
        IQueryHandler<GetPoolMembersQuery, IReadOnlyList<PoolMemberDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetPoolMembersQuery(id), ct);
        return result.ToHttpResult();
    })
    .WithName("GetPoolMembers")
    .WithSummary("List a pool's candidates.")
    .Produces<IReadOnlyList<PoolMemberDto>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/pools", async (
        CreatePoolBody body,
        ICommandHandler<CreateTalentPoolCommand, TalentPoolDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new CreateTalentPoolCommand(body.Name), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("CreateTalentPool")
    .WithSummary("Create a talent pool. Requires an admin (write) role.")
    .Produces<TalentPoolDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

v1.MapPost("/pools/{id:guid}/members/{candidateId:guid}", async (
        Guid id,
        Guid candidateId,
        ICommandHandler<AddToPoolCommand, bool> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new AddToPoolCommand(id, candidateId), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("AddToPool")
    .WithSummary("Add a candidate to a pool. Requires an admin (write) role.")
    .Produces<bool>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapDelete("/pools/{id:guid}/members/{candidateId:guid}", async (
        Guid id,
        Guid candidateId,
        ICommandHandler<RemoveFromPoolCommand, bool> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new RemoveFromPoolCommand(id, candidateId), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RemoveFromPool")
    .WithSummary("Remove a candidate from a pool. Requires an admin (write) role.")
    .Produces<bool>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

// --- Recruiter notes on a candidate ---
v1.MapGet("/{id:guid}/notes", async (
        Guid id,
        IQueryHandler<GetCandidateNotesQuery, IReadOnlyList<CandidateNoteDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetCandidateNotesQuery(id), ct);
        return result.ToHttpResult();
    })
    .WithName("GetCandidateNotes")
    .WithSummary("List a candidate's recruiter notes.")
    .Produces<IReadOnlyList<CandidateNoteDto>>(StatusCodes.Status200OK);

v1.MapPost("/{id:guid}/notes", async (
        Guid id,
        AddNoteBody body,
        ICommandHandler<AddCandidateNoteCommand, CandidateNoteDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new AddCandidateNoteCommand(id, body.Author, body.Body), ct);
        return result.ToCreatedResult(dto => $"/v1/candidates/{id}/notes/{dto.Id}");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("AddCandidateNote")
    .WithSummary("Add a recruiter note to a candidate. Requires an admin (write) role.")
    .Produces<CandidateNoteDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapDelete("/notes/{noteId:guid}", async (
        Guid noteId,
        ICommandHandler<RemoveCandidateNoteCommand, bool> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new RemoveCandidateNoteCommand(noteId), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RemoveCandidateNote")
    .WithSummary("Remove a recruiter note. Requires an admin (write) role.")
    .Produces(StatusCodes.Status204NoContent)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

// --- Tags / labels on a candidate ---
v1.MapGet("/{id:guid}/tags", async (
        Guid id,
        IQueryHandler<GetCandidateTagsQuery, IReadOnlyList<string>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetCandidateTagsQuery(id), ct);
        return result.ToHttpResult();
    })
    .WithName("GetCandidateTags")
    .WithSummary("List a candidate's tags.")
    .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK);

v1.MapPost("/{id:guid}/tags", async (
        Guid id,
        AddTagBody body,
        ICommandHandler<AddCandidateTagCommand, IReadOnlyList<string>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new AddCandidateTagCommand(id, body.Label), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("AddCandidateTag")
    .WithSummary("Add a tag to a candidate (idempotent). Requires an admin (write) role.")
    .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapDelete("/{id:guid}/tags/{label}", async (
        Guid id,
        string label,
        ICommandHandler<RemoveCandidateTagCommand, IReadOnlyList<string>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new RemoveCandidateTagCommand(id, label), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RemoveCandidateTag")
    .WithSummary("Remove a tag from a candidate. Requires an admin (write) role.")
    .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

app.Run();

/// <summary>Request body for creating a talent pool.</summary>
/// <param name="Name">Pool name.</param>
internal sealed record CreatePoolBody(string Name);

/// <summary>Request body for adding a recruiter note.</summary>
/// <param name="Author">Author display name.</param>
/// <param name="Body">Note body.</param>
internal sealed record AddNoteBody(string? Author, string Body);

/// <summary>Request body for adding a tag.</summary>
/// <param name="Label">The tag label.</param>
internal sealed record AddTagBody(string Label);

/// <summary>Exposed so integration tests can use <c>WebApplicationFactory</c> (charter Part 14).</summary>
public partial class Program;
