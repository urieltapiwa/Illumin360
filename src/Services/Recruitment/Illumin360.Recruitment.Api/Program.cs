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

// --- Scheduled job-alert digests (enabled by default; interval via JobAlerts:IntervalSeconds) ---
if (builder.Configuration.GetValue<bool?>("JobAlerts:Enabled") ?? true)
{
    builder.Services.AddHostedService<Illumin360.Recruitment.Api.JobAlertScheduler>();
}

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
    await CrmSeeder.SeedAsync(db, CancellationToken.None);
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

// --- Interviews & scheduling ---
v1.MapGet("/applications/{applicationId:guid}/interviews", async (
        Guid applicationId,
        IQueryHandler<GetInterviewsQuery, IReadOnlyList<InterviewDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetInterviewsQuery(applicationId), ct);
        return result.ToHttpResult();
    })
    .WithName("GetInterviews")
    .WithSummary("List an application's interviews.")
    .Produces<IReadOnlyList<InterviewDto>>(StatusCodes.Status200OK);

v1.MapPost("/applications/{applicationId:guid}/interviews", async (
        Guid applicationId,
        ScheduleInterviewBody body,
        ICommandHandler<ScheduleInterviewCommand, InterviewDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(
            new ScheduleInterviewCommand(applicationId, body.ScheduledAt, body.DurationMinutes, body.Location), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("ScheduleInterview")
    .WithSummary("Schedule an interview for an application. Requires an admin (write) role.")
    .Produces<InterviewDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

v1.MapPost("/interviews/{id:guid}/feedback", async (
        Guid id,
        InterviewFeedbackBody body,
        ICommandHandler<RecordInterviewFeedbackCommand, InterviewDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new RecordInterviewFeedbackCommand(id, body.Rating, body.Comment), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RecordInterviewFeedback")
    .WithSummary("Record a scorecard and complete an interview. Requires an admin (write) role.")
    .Produces<InterviewDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/interviews/{id:guid}/cancel", async (
        Guid id,
        ICommandHandler<CancelInterviewCommand, InterviewDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new CancelInterviewCommand(id), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("CancelInterview")
    .WithSummary("Cancel a scheduled interview. Requires an admin (write) role.")
    .Produces<InterviewDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapGet("/interviews/{id:guid}/ics", async (
        Guid id,
        IQueryHandler<GetInterviewIcsQuery, string> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetInterviewIcsQuery(id), ct);
        return result.IsSuccess
            ? Results.Text(result.Value!, "text/calendar")
            : result.ToHttpResult();
    })
    .WithName("GetInterviewIcs")
    .WithSummary("Download an interview's calendar (.ics) invite.")
    .Produces(StatusCodes.Status200OK)
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

// --- Offers: employment offers on an application (draft → sent → accepted/declined) ---
v1.MapGet("/applications/{applicationId:guid}/offers", async (
        Guid applicationId,
        IQueryHandler<GetOffersQuery, IReadOnlyList<OfferDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetOffersQuery(applicationId), ct);
        return result.ToHttpResult();
    })
    .WithName("GetOffers")
    .WithSummary("List an application's employment offers.")
    .Produces<IReadOnlyList<OfferDto>>(StatusCodes.Status200OK);

v1.MapPost("/applications/{applicationId:guid}/offers", async (
        Guid applicationId,
        CreateOfferBody body,
        ICommandHandler<CreateOfferCommand, OfferDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(
            new CreateOfferCommand(applicationId, body.Title, body.SalaryAmount, body.Currency, body.StartDate, body.Notes), ct);
        return result.ToCreatedResult(dto => $"/v1/recruitment/offers/{dto.Id}");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("CreateOffer")
    .WithSummary("Draft an employment offer for an application. Requires an admin (write) role.")
    .Produces<OfferDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

v1.MapPost("/offers/{id:guid}/send", async (
        Guid id,
        ICommandHandler<TransitionOfferCommand, OfferDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new TransitionOfferCommand(id, OfferAction.Send), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("SendOffer")
    .WithSummary("Extend a draft offer to the candidate. Requires an admin (write) role.")
    .Produces<OfferDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/offers/{id:guid}/withdraw", async (
        Guid id,
        ICommandHandler<TransitionOfferCommand, OfferDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new TransitionOfferCommand(id, OfferAction.Withdraw), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("WithdrawOffer")
    .WithSummary("Withdraw an offer before a decision. Requires an admin (write) role.")
    .Produces<OfferDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/offers/{id:guid}/accept", async (
        Guid id,
        ICommandHandler<TransitionOfferCommand, OfferDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new TransitionOfferCommand(id, OfferAction.Accept), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.ProfessionalPolicy)
    .WithName("AcceptOffer")
    .WithSummary("Candidate accepts a sent offer. Requires a signed-in talent.")
    .Produces<OfferDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/offers/{id:guid}/decline", async (
        Guid id,
        ICommandHandler<TransitionOfferCommand, OfferDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new TransitionOfferCommand(id, OfferAction.Decline), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.ProfessionalPolicy)
    .WithName("DeclineOffer")
    .WithSummary("Candidate declines a sent offer. Requires a signed-in talent.")
    .Produces<OfferDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

// --- Onboarding checklist on hire ---
v1.MapGet("/applications/{applicationId:guid}/onboarding", async (
        Guid applicationId,
        IQueryHandler<GetOnboardingQuery, OnboardingChecklistDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetOnboardingQuery(applicationId), ct);
        return result.ToHttpResult();
    })
    .WithName("GetOnboarding")
    .WithSummary("Get the onboarding checklist for an application.")
    .Produces<OnboardingChecklistDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/applications/{applicationId:guid}/onboarding", async (
        Guid applicationId,
        StartOnboardingBody body,
        ICommandHandler<StartOnboardingCommand, OnboardingChecklistDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new StartOnboardingCommand(applicationId, body.RoleTitle), ct);
        return result.ToCreatedResult(dto => $"/v1/recruitment/applications/{applicationId}/onboarding");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("StartOnboarding")
    .WithSummary("Start an onboarding checklist (with default tasks) for a hired application. Requires an admin (write) role.")
    .Produces<OnboardingChecklistDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/onboarding/tasks/{taskId:guid}/toggle", async (
        Guid taskId,
        ToggleTaskBody body,
        ICommandHandler<ToggleOnboardingTaskCommand, OnboardingTaskDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new ToggleOnboardingTaskCommand(taskId, body.Done), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("ToggleOnboardingTask")
    .WithSummary("Mark an onboarding task done/undone. Requires an admin (write) role.")
    .Produces<OnboardingTaskDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/onboarding/{checklistId:guid}/tasks", async (
        Guid checklistId,
        AddTaskBody body,
        ICommandHandler<AddOnboardingTaskCommand, OnboardingTaskDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new AddOnboardingTaskCommand(checklistId, body.Label), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("AddOnboardingTask")
    .WithSummary("Add a custom task to an onboarding checklist. Requires an admin (write) role.")
    .Produces<OnboardingTaskDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapDelete("/onboarding/tasks/{taskId:guid}", async (
        Guid taskId,
        ICommandHandler<RemoveOnboardingTaskCommand, bool> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new RemoveOnboardingTaskCommand(taskId), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RemoveOnboardingTask")
    .WithSummary("Remove a task from an onboarding checklist. Requires an admin (write) role.")
    .Produces(StatusCodes.Status204NoContent)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

// --- Public branded careers pages (SEO-friendly server-rendered HTML; no auth) ---
// Served publicly at /careers via the gateway (which rewrites /careers/** → /v1/recruitment/careers/**).
const string careersBrand = "Illumin360";
const string careersBasePath = "/careers";

v1.MapGet("/careers", async (
        IQueryHandler<GetRecruitmentRequestsQuery, IReadOnlyList<RecruitmentRequestDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetRecruitmentRequestsQuery(null, "open", 1, 100), ct);
        var roles = result.IsSuccess ? result.Value! : [];
        return Results.Content(CareersHtml.RenderIndex(roles, careersBrand, careersBasePath), "text/html; charset=utf-8");
    })
    .WithName("CareersIndex")
    .WithSummary("Public branded careers landing page listing open roles (HTML).")
    .Produces(StatusCodes.Status200OK, contentType: "text/html");

v1.MapGet("/careers/{id:guid}", async (
        Guid id,
        IQueryHandler<GetRecruitmentRequestByIdQuery, RecruitmentRequestDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetRecruitmentRequestByIdQuery(id), ct);
        if (result.IsFailure)
        {
            return Results.Content(
                CareersHtml.RenderIndex([], careersBrand, careersBasePath), "text/html; charset=utf-8", statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Content(CareersHtml.RenderJob(result.Value!, careersBrand, careersBasePath), "text/html; charset=utf-8");
    })
    .WithName("CareersJob")
    .WithSummary("Public branded careers detail page for a single role (HTML + JobPosting JSON-LD).")
    .Produces(StatusCodes.Status200OK, contentType: "text/html");

// --- Recruiter CRM: client companies + contacts (internal recruiter tooling) ---
v1.MapGet("/clients", async (
        string? status,
        IQueryHandler<ListClientsQuery, IReadOnlyList<ClientDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new ListClientsQuery(status), ct);
        return result.ToHttpResult();
    })
    .WithName("ListClients")
    .WithSummary("List CRM clients, optionally filtered by status (prospect/active/inactive).")
    .Produces<IReadOnlyList<ClientDto>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest);

v1.MapGet("/clients/{id:guid}", async (
        Guid id,
        IQueryHandler<GetClientQuery, ClientDetailDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetClientQuery(id), ct);
        return result.ToHttpResult();
    })
    .WithName("GetClient")
    .WithSummary("Get a CRM client with its contacts.")
    .Produces<ClientDetailDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/clients", async (
        CreateClientBody body,
        ICommandHandler<CreateClientCommand, ClientDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new CreateClientCommand(body.Name, body.Industry, body.City, body.Notes), ct);
        return result.ToCreatedResult(dto => $"/v1/recruitment/clients/{dto.Id}");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("CreateClient")
    .WithSummary("Create a CRM client. Requires an admin (write) role.")
    .Produces<ClientDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

v1.MapPost("/clients/{id:guid}/status", async (
        Guid id,
        ChangeClientStatusBody body,
        ICommandHandler<ChangeClientStatusCommand, ClientDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new ChangeClientStatusCommand(id, body.Status), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("ChangeClientStatus")
    .WithSummary("Change a client's relationship status. Requires an admin (write) role.")
    .Produces<ClientDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/clients/{id:guid}/contacts", async (
        Guid id,
        AddContactBody body,
        ICommandHandler<AddClientContactCommand, ClientContactDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(
            new AddClientContactCommand(id, body.Name, body.Title, body.Email, body.Phone, body.IsPrimary), ct);
        return result.ToCreatedResult(dto => $"/v1/recruitment/clients/{id}/contacts/{dto.Id}");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("AddClientContact")
    .WithSummary("Add a contact to a client. Requires an admin (write) role.")
    .Produces<ClientContactDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapDelete("/clients/{id:guid}/contacts/{contactId:guid}", async (
        Guid contactId,
        ICommandHandler<RemoveClientContactCommand, bool> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new RemoveClientContactCommand(contactId), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RemoveClientContact")
    .WithSummary("Remove a contact from a client. Requires an admin (write) role.")
    .Produces(StatusCodes.Status204NoContent)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

app.Run();

/// <summary>Request body for creating a CRM client.</summary>
/// <param name="Name">Company name.</param>
/// <param name="Industry">Optional industry.</param>
/// <param name="City">Optional city.</param>
/// <param name="Notes">Optional notes.</param>
internal sealed record CreateClientBody(string Name, string? Industry, string? City, string? Notes);

/// <summary>Request body for changing a client's status.</summary>
/// <param name="Status">The new status (prospect/active/inactive).</param>
internal sealed record ChangeClientStatusBody(string Status);

/// <summary>Request body for adding a client contact.</summary>
/// <param name="Name">Contact name.</param>
/// <param name="Title">Optional job title.</param>
/// <param name="Email">Optional email.</param>
/// <param name="Phone">Optional phone.</param>
/// <param name="IsPrimary">Whether this is the primary contact.</param>
internal sealed record AddContactBody(string Name, string? Title, string? Email, string? Phone, bool IsPrimary);

/// <summary>Request body for applying to a recruitment request.</summary>
/// <param name="TalentId">The applying talent's id.</param>
/// <param name="TalentType">Talent type (<c>student</c>/<c>professional</c>); defaults to professional.</param>
internal sealed record ApplyToRequestBody(Guid TalentId, string? TalentType);

/// <summary>Request body for starting an onboarding checklist.</summary>
/// <param name="RoleTitle">The hired role title.</param>
internal sealed record StartOnboardingBody(string RoleTitle);

/// <summary>Request body for toggling an onboarding task.</summary>
/// <param name="Done">Whether the task is complete.</param>
internal sealed record ToggleTaskBody(bool Done);

/// <summary>Request body for adding a custom onboarding task.</summary>
/// <param name="Label">The task label.</param>
internal sealed record AddTaskBody(string Label);

/// <summary>Request body for drafting an employment offer.</summary>
/// <param name="Title">Role title.</param>
/// <param name="SalaryAmount">Salary amount.</param>
/// <param name="Currency">Optional currency code (defaults to NAD).</param>
/// <param name="StartDate">Proposed start date (yyyy-MM-dd).</param>
/// <param name="Notes">Optional notes.</param>
internal sealed record CreateOfferBody(string Title, decimal SalaryAmount, string? Currency, DateOnly StartDate, string? Notes);

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

/// <summary>Request body for scheduling an interview.</summary>
/// <param name="ScheduledAt">Start time (UTC).</param>
/// <param name="DurationMinutes">Duration in minutes.</param>
/// <param name="Location">Location or mode.</param>
internal sealed record ScheduleInterviewBody(DateTimeOffset ScheduledAt, int DurationMinutes, string Location);

/// <summary>Request body for recording interview feedback.</summary>
/// <param name="Rating">Scorecard rating (1–5).</param>
/// <param name="Comment">Optional comment.</param>
internal sealed record InterviewFeedbackBody(int Rating, string? Comment);

/// <summary>Exposed so integration tests can use <c>WebApplicationFactory</c> (charter Part 14).</summary>
public partial class Program;
