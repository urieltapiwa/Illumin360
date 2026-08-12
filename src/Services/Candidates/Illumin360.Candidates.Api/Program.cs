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

// --- Embedding backend for semantic matching ---
// Default: deterministic hashing (no external calls, no data egress). A hosted model is used ONLY when a
// tenant opts in via Matching:Embeddings (Provider=Hosted, Enabled=true, Endpoint set) — the data-egress gate.
var embeddingOptions = builder.Configuration.GetSection("Matching:Embeddings").Get<Illumin360.Matching.EmbeddingOptions>()
    ?? new Illumin360.Matching.EmbeddingOptions();
if (embeddingOptions.UseHosted)
{
    builder.Services.AddSingleton(embeddingOptions);
    builder.Services.AddHttpClient<Illumin360.Matching.IEmbeddingClient, Illumin360.Matching.HostedEmbeddingClient>();
}
else
{
    builder.Services.AddSingleton<Illumin360.Matching.IEmbeddingClient>(new Illumin360.Matching.HashingEmbeddingProvider(embeddingOptions.Dimensions));
}

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
        bool? blind,
        IQueryHandler<SearchCandidatesQuery, CandidateSearchResultDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new SearchCandidatesQuery(city, availability, q, hasCv, page ?? 1, pageSize ?? 20, blind ?? false), ct);
        return result.ToHttpResult();
    })
    .WithName("SearchCandidates")
    .WithSummary("Faceted candidate search over city, availability, keyword and CV presence — returns matches, total and facet counts. Pass blind=true to anonymise name + nationality (blind screening).")
    .Produces<CandidateSearchResultDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest);

v1.MapDelete("/{id:guid}", async (
        Guid id,
        ICommandHandler<EraseCandidateCommand, bool> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new EraseCandidateCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("EraseCandidate")
    .WithSummary("GDPR right-to-be-forgotten: permanently erase a candidate and all their data. Requires an admin (write) role.")
    .Produces(StatusCodes.Status204NoContent)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapGet("/{id:guid}/export", async (
        Guid id,
        IQueryHandler<GetCandidateExportQuery, CandidateExportDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetCandidateExportQuery(id), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("ExportCandidateData")
    .WithSummary("GDPR subject-access export of a candidate's data (profile, notes, tags, CV metadata). Requires an admin (write) role.")
    .Produces<CandidateExportDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapGet("/duplicates", async (
        bool? sameCityOnly,
        IQueryHandler<FindDuplicateCandidatesQuery, IReadOnlyList<DuplicateGroupDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new FindDuplicateCandidatesQuery(sameCityOnly ?? false), ct);
        return result.ToHttpResult();
    })
    .WithName("FindDuplicateCandidates")
    .WithSummary("Find suspected-duplicate candidates (shared name, optionally same city).")
    .Produces<IReadOnlyList<DuplicateGroupDto>>(StatusCodes.Status200OK);

v1.MapGet("/{id:guid}/similar", async (
        Guid id,
        int? take,
        IQueryHandler<GetSimilarCandidatesQuery, IReadOnlyList<SimilarCandidateDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetSimilarCandidatesQuery(id, take ?? 5), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminPolicy)
    .WithName("GetSimilarCandidates")
    .WithSummary("Find candidates most similar to a seed candidate (\"more like this\"). Requires an admin role.")
    .Produces<IReadOnlyList<SimilarCandidateDto>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapGet("/{id:guid}/semantic-similar", async (
        Guid id,
        int? take,
        IConfiguration config,
        IQueryHandler<GetSemanticSimilarCandidatesQuery, IReadOnlyList<SimilarCandidateDto>> handler,
        CancellationToken ct) =>
    {
        // Feature-flagged off by default (Matching:SemanticEnabled). When off, return an empty set so the
        // UI can hide the section without special-casing errors.
        if (!config.GetValue("Matching:SemanticEnabled", false))
        {
            return Results.Ok(Array.Empty<SimilarCandidateDto>());
        }

        var result = await handler.HandleAsync(new GetSemanticSimilarCandidatesQuery(id, take ?? 5), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminPolicy)
    .WithName("GetSemanticSimilarCandidates")
    .WithSummary("Semantic \"more like this\" over embeddings (flag Matching:SemanticEnabled, off by default; hashing provider v1). Requires an admin role.")
    .Produces<IReadOnlyList<SimilarCandidateDto>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapGet("/diversity", async (
        IQueryHandler<GetDiversityReportQuery, DiversityReportDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetDiversityReportQuery(), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminPolicy)
    .WithName("GetDiversityReport")
    .WithSummary("Anonymised diversity/EEO report over the candidate pool (counts by nationality/city/availability). Requires an admin role.")
    .Produces<DiversityReportDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

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

v1.MapPost("/import", async (
        ImportCandidatesCommand command,
        ICommandHandler<ImportCandidatesCommand, ImportResultDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("ImportCandidates")
    .WithSummary("Bulk-import candidates from CSV (header: firstName,lastName,city,nationality[,availability,headline]). Dedupes by name+city. Requires an admin (write) role.")
    .Produces<ImportResultDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

v1.MapPost("/intake/email", async (
        IngestEmailResumeCommand command,
        ICommandHandler<IngestEmailResumeCommand, EmailIntakeResultDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("IngestEmailResume")
    .WithSummary("Email-to-ATS intake: parse a résumé emailed to the company inbox into a candidate stub (dedupes by name+city; attaches the CV when it's a supported type). Called by a mailbox poller with an admin (write) identity.")
    .Produces<EmailIntakeResultDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

// --- Admin-defined custom fields on candidate records ---
v1.MapGet("/custom-fields", async (
        IQueryHandler<GetCustomFieldsQuery, IReadOnlyList<CustomFieldDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetCustomFieldsQuery(), ct);
        return result.ToHttpResult();
    })
    .WithName("GetCustomFields")
    .WithSummary("List admin-defined candidate custom-field definitions.")
    .Produces<IReadOnlyList<CustomFieldDto>>(StatusCodes.Status200OK);

v1.MapPost("/custom-fields", async (
        AddCustomFieldCommand command,
        ICommandHandler<AddCustomFieldCommand, CustomFieldDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToCreatedResult(dto => $"/v1/candidates/custom-fields/{dto.Id}");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("AddCustomField")
    .WithSummary("Define a candidate custom field. Requires an admin (write) role.")
    .Produces<CustomFieldDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapDelete("/custom-fields/{id:guid}", async (
        Guid id,
        ICommandHandler<RemoveCustomFieldCommand, bool> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new RemoveCustomFieldCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RemoveCustomField")
    .WithSummary("Remove a candidate custom-field definition. Requires an admin (write) role.")
    .Produces(StatusCodes.Status204NoContent)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapGet("/{id:guid}/custom-values", async (
        Guid id,
        IQueryHandler<GetCandidateCustomValuesQuery, IReadOnlyList<CustomValueDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetCandidateCustomValuesQuery(id), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminPolicy)
    .WithName("GetCandidateCustomValues")
    .WithSummary("Get a candidate's custom-field values (every defined field, value empty when unset). Requires an admin role.")
    .Produces<IReadOnlyList<CustomValueDto>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

v1.MapPut("/{id:guid}/custom-values", async (
        Guid id,
        SetCustomValuesBody body,
        ICommandHandler<SetCandidateCustomValuesCommand, int> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new SetCandidateCustomValuesCommand(id, body.Values ?? []), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("SetCandidateCustomValues")
    .WithSummary("Set (replace) a candidate's custom-field values. Requires an admin (write) role.")
    .Produces<int>(StatusCodes.Status200OK)
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

/// <summary>Request body for setting a candidate's custom-field values.</summary>
/// <param name="Values">The values ({definitionId, value}).</param>
internal sealed record SetCustomValuesBody(IReadOnlyList<CustomValueInput>? Values);

/// <summary>Exposed so integration tests can use <c>WebApplicationFactory</c> (charter Part 14).</summary>
public partial class Program;
