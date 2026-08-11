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

v1.MapGet("/metrics/hiring", async (
        IQueryHandler<GetHiringMetricsQuery, HiringMetricsDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetHiringMetricsQuery(), ct);
        return result.ToHttpResult();
    })
    .WithName("GetHiringMetrics")
    .WithSummary("Time-to-hire (avg/median days) and source-of-hire (by talent type).")
    .Produces<HiringMetricsDto>(StatusCodes.Status200OK);

// --- Reports export (CSV) ---
v1.MapGet("/reports/source-of-hire.csv", async (
        IQueryHandler<GetHiringMetricsQuery, HiringMetricsDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetHiringMetricsQuery(), ct);
        return result.IsSuccess
            ? Results.Text(ReportsCsv.SourceOfHire(result.Value!), "text/csv")
            : result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminPolicy)
    .WithName("SourceOfHireCsv")
    .WithSummary("Download the source-of-hire report as CSV. Requires an admin role.")
    .Produces(StatusCodes.Status200OK, contentType: "text/csv");

v1.MapGet("/reports/funnel.csv", async (
        IQueryHandler<GetRecruitmentStatsQuery, RecruitmentStatsDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetRecruitmentStatsQuery(), ct);
        return result.IsSuccess
            ? Results.Text(ReportsCsv.Funnel(result.Value!), "text/csv")
            : result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminPolicy)
    .WithName("FunnelCsv")
    .WithSummary("Download the pipeline-funnel report as CSV. Requires an admin role.")
    .Produces(StatusCodes.Status200OK, contentType: "text/csv");

v1.MapGet("/reports/source-of-hire.pdf", async (
        IQueryHandler<GetHiringMetricsQuery, HiringMetricsDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetHiringMetricsQuery(), ct);
        if (result.IsFailure)
        {
            return result.ToHttpResult();
        }

        var m = result.Value!;
        var lines = new List<string>
        {
            $"Total hires: {m.Hires}    Avg time-to-hire: {m.AvgTimeToHireDays}d    Median: {m.MedianTimeToHireDays}d",
            string.Empty,
            "Source                Applications   Hires   Hire rate",
        };
        lines.AddRange(m.BySource.Select(s =>
        {
            var rate = s.Applications > 0 ? Math.Round(100.0 * s.Hires / s.Applications) : 0;
            return $"{s.Source,-20}  {s.Applications,12}   {s.Hires,5}   {rate,7}%";
        }));
        return Results.Bytes(ReportsPdf.Render("Illumin360 — Source of Hire", lines), "application/pdf");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminPolicy)
    .WithName("SourceOfHirePdf")
    .WithSummary("Download the source-of-hire report as PDF. Requires an admin role.")
    .Produces(StatusCodes.Status200OK, contentType: "application/pdf");

v1.MapGet("/reports/funnel.pdf", async (
        IQueryHandler<GetRecruitmentStatsQuery, RecruitmentStatsDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetRecruitmentStatsQuery(), ct);
        if (result.IsFailure)
        {
            return result.ToHttpResult();
        }

        var lines = new List<string> { "Stage                 Count", string.Empty };
        lines.AddRange(result.Value!.Funnel.Select(f => $"{f.Label,-20}  {f.Count,6}"));
        return Results.Bytes(ReportsPdf.Render("Illumin360 — Pipeline Funnel", lines), "application/pdf");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminPolicy)
    .WithName("FunnelPdf")
    .WithSummary("Download the pipeline-funnel report as PDF. Requires an admin role.")
    .Produces(StatusCodes.Status200OK, contentType: "application/pdf");

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

// --- Requisition enrichment: salary range, employment type, remote flag + tags ---
v1.MapGet("/requests/{id:guid}/details", async (
        Guid id,
        IQueryHandler<GetRequisitionDetailQuery, RequisitionDetailDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetRequisitionDetailQuery(id), ct);
        return result.ToHttpResult();
    })
    .WithName("GetRequisitionDetail")
    .WithSummary("Get a requisition's salary range, employment type, remote flag and tags.")
    .Produces<RequisitionDetailDto>(StatusCodes.Status200OK);

v1.MapPut("/requests/{id:guid}/details", async (
        Guid id,
        RequisitionDetailBody body,
        ICommandHandler<SetRequisitionDetailCommand, RequisitionDetailDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(
            new SetRequisitionDetailCommand(id, body.SalaryMin, body.SalaryMax, body.Currency, body.EmploymentType, body.Remote), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("SetRequisitionDetail")
    .WithSummary("Set a requisition's salary range, employment type and remote flag. Requires an admin (write) role.")
    .Produces<RequisitionDetailDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/requests/{id:guid}/tags", async (
        Guid id,
        RequisitionTagBody body,
        ICommandHandler<AddRequisitionTagCommand, IReadOnlyList<string>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new AddRequisitionTagCommand(id, body.Label), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("AddRequisitionTag")
    .WithSummary("Add a category tag to a requisition (idempotent). Requires an admin (write) role.")
    .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapDelete("/requests/{id:guid}/tags/{label}", async (
        Guid id,
        string label,
        ICommandHandler<RemoveRequisitionTagCommand, IReadOnlyList<string>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new RemoveRequisitionTagCommand(id, label), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RemoveRequisitionTag")
    .WithSummary("Remove a category tag from a requisition. Requires an admin (write) role.")
    .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

// --- Requisition approval workflow (draft → submitted → approved/rejected) ---
v1.MapGet("/requests/{id:guid}/approval", async (
        Guid id,
        IQueryHandler<GetApprovalQuery, ApprovalDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetApprovalQuery(id), ct);
        return result.ToHttpResult();
    })
    .WithName("GetRequisitionApproval")
    .WithSummary("Get a requisition's approval state.")
    .Produces<ApprovalDto>(StatusCodes.Status200OK);

v1.MapPost("/requests/{id:guid}/approval/submit", async (
        Guid id,
        ICommandHandler<TransitionApprovalCommand, ApprovalDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new TransitionApprovalCommand(id, ApprovalAction.Submit, null, null), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("SubmitRequisitionForApproval")
    .WithSummary("Submit a draft/rejected requisition for approval. Requires an admin (write) role.")
    .Produces<ApprovalDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/requests/{id:guid}/approval/approve", async (
        Guid id,
        ApprovalDecisionBody body,
        ICommandHandler<TransitionApprovalCommand, ApprovalDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new TransitionApprovalCommand(id, ApprovalAction.Approve, body.Approver, null), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("ApproveRequisition")
    .WithSummary("Approve a submitted requisition. Requires an admin (write) role.")
    .Produces<ApprovalDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/requests/{id:guid}/approval/reject", async (
        Guid id,
        ApprovalRejectBody body,
        ICommandHandler<TransitionApprovalCommand, ApprovalDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new TransitionApprovalCommand(id, ApprovalAction.Reject, body.Approver, body.Reason), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RejectRequisition")
    .WithSummary("Reject a submitted requisition with a reason. Requires an admin (write) role.")
    .Produces<ApprovalDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

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
    .WithSummary("Download an interview's calendar (.ics) invite (lists the panel as ATTENDEEs).")
    .Produces(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapGet("/talents/{talentId:guid}/calendar.ics", async (
        Guid talentId,
        IQueryHandler<GetTalentCalendarFeedQuery, string> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetTalentCalendarFeedQuery(talentId), ct);
        return result.IsSuccess
            ? Results.Text(result.Value!, "text/calendar")
            : result.ToHttpResult();
    })
    .WithName("GetTalentCalendarFeed")
    .WithSummary("Subscribable iCalendar feed of a talent's interviews (add to Google/Outlook by URL).")
    .Produces(StatusCodes.Status200OK, contentType: "text/calendar");

// --- Panel interviews: interview attendees ---
v1.MapGet("/interviews/{id:guid}/attendees", async (
        Guid id,
        IQueryHandler<GetInterviewAttendeesQuery, IReadOnlyList<InterviewAttendeeDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetInterviewAttendeesQuery(id), ct);
        return result.ToHttpResult();
    })
    .WithName("GetInterviewAttendees")
    .WithSummary("List an interview's panel attendees.")
    .Produces<IReadOnlyList<InterviewAttendeeDto>>(StatusCodes.Status200OK);

v1.MapPost("/interviews/{id:guid}/attendees", async (
        Guid id,
        AttendeeBody body,
        ICommandHandler<AddInterviewAttendeeCommand, InterviewAttendeeDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new AddInterviewAttendeeCommand(id, body.Name, body.Email, body.Role), ct);
        return result.ToCreatedResult(dto => $"/v1/recruitment/interviews/{id}/attendees/{dto.Id}");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("AddInterviewAttendee")
    .WithSummary("Add a panel attendee to an interview. Requires an admin (write) role.")
    .Produces<InterviewAttendeeDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapDelete("/interviews/attendees/{attendeeId:guid}", async (
        Guid attendeeId,
        ICommandHandler<RemoveInterviewAttendeeCommand, bool> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new RemoveInterviewAttendeeCommand(attendeeId), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RemoveInterviewAttendee")
    .WithSummary("Remove a panel attendee. Requires an admin (write) role.")
    .Produces(StatusCodes.Status204NoContent)
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

// --- Job templates (reusable requisitions) ---
v1.MapGet("/templates", async (
        IQueryHandler<GetJobTemplatesQuery, IReadOnlyList<JobTemplateDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetJobTemplatesQuery(), ct);
        return result.ToHttpResult();
    })
    .WithName("GetJobTemplates")
    .WithSummary("List reusable job templates.")
    .Produces<IReadOnlyList<JobTemplateDto>>(StatusCodes.Status200OK);

v1.MapPost("/templates", async (
        JobTemplateBody body,
        ICommandHandler<CreateJobTemplateCommand, JobTemplateDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(
            new CreateJobTemplateCommand(body.Name, body.Title, body.City, body.Positions, body.SalaryMin, body.SalaryMax, body.Currency, body.EmploymentType, body.Remote, body.Tags), ct);
        return result.ToCreatedResult(dto => $"/v1/recruitment/templates/{dto.Id}");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("CreateJobTemplate")
    .WithSummary("Create a reusable job template. Requires an admin (write) role.")
    .Produces<JobTemplateDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapDelete("/templates/{id:guid}", async (
        Guid id,
        ICommandHandler<DeleteJobTemplateCommand, bool> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new DeleteJobTemplateCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("DeleteJobTemplate")
    .WithSummary("Delete a job template. Requires an admin (write) role.")
    .Produces(StatusCodes.Status204NoContent)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/templates/{id:guid}/use", async (
        Guid id,
        UseTemplateBody body,
        ICommandHandler<UseJobTemplateCommand, RecruitmentRequestDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new UseJobTemplateCommand(id, body.CompanyId), ct);
        return result.ToCreatedResult(dto => $"/v1/recruitment/requests/{dto.Id}");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("UseJobTemplate")
    .WithSummary("Create a new requisition (with enrichment + tags) from a template. Requires an admin (write) role.")
    .Produces<RecruitmentRequestDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

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

v1.MapPost("/offers/{id:guid}/sign", async (
        Guid id,
        SignOfferBody body,
        ICommandHandler<SignOfferCommand, OfferDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new SignOfferCommand(id, body.SignerName), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.ProfessionalPolicy)
    .WithName("SignOffer")
    .WithSummary("Candidate e-signs (and accepts) a sent offer. Requires a signed-in talent.")
    .Produces<OfferDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapGet("/offers/{id:guid}/letter", async (
        Guid id,
        IQueryHandler<GetOfferQuery, OfferDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetOfferQuery(id), ct);
        return result.IsSuccess
            ? Results.Content(OfferLetterHtml.Render(result.Value!, "Illumin360"), "text/html; charset=utf-8")
            : result.ToHttpResult();
    })
    .WithName("GetOfferLetter")
    .WithSummary("Render the offer letter (HTML) for a single offer, with its e-signature block.")
    .Produces(StatusCodes.Status200OK, contentType: "text/html")
    .ProducesProblem(StatusCodes.Status404NotFound);

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

// --- Bulk email campaigns ---
v1.MapGet("/campaigns", async (
        IQueryHandler<GetCampaignsQuery, IReadOnlyList<CampaignDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetCampaignsQuery(), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("GetCampaigns")
    .WithSummary("List email campaigns. Requires an admin (write) role.")
    .Produces<IReadOnlyList<CampaignDto>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

v1.MapPost("/campaigns", async (
        CampaignBody body,
        ICommandHandler<CreateCampaignCommand, CampaignDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new CreateCampaignCommand(body.Name, body.Subject, body.Body), ct);
        return result.ToCreatedResult(dto => $"/v1/recruitment/campaigns/{dto.Id}");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("CreateCampaign")
    .WithSummary("Create a draft email campaign. Requires an admin (write) role.")
    .Produces<CampaignDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

v1.MapGet("/campaigns/{id:guid}", async (
        Guid id,
        IQueryHandler<GetCampaignQuery, CampaignDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetCampaignQuery(id), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("GetCampaign")
    .WithSummary("Get a campaign with its recipients. Requires an admin (write) role.")
    .Produces<CampaignDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/campaigns/{id:guid}/recipients", async (
        Guid id,
        RecipientBody body,
        ICommandHandler<AddCampaignRecipientCommand, CampaignDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new AddCampaignRecipientCommand(id, body.Email), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("AddCampaignRecipient")
    .WithSummary("Add a recipient to a draft campaign. Requires an admin (write) role.")
    .Produces<CampaignDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapDelete("/campaigns/{id:guid}/recipients/{email}", async (
        Guid id,
        string email,
        ICommandHandler<RemoveCampaignRecipientCommand, CampaignDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new RemoveCampaignRecipientCommand(id, email), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RemoveCampaignRecipient")
    .WithSummary("Remove a recipient from a draft campaign. Requires an admin (write) role.")
    .Produces<CampaignDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/campaigns/{id:guid}/send", async (
        Guid id,
        ICommandHandler<SendCampaignCommand, CampaignDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new SendCampaignCommand(id), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("SendCampaign")
    .WithSummary("Send a draft campaign to its recipients. Requires an admin (write) role.")
    .Produces<CampaignDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

// --- In-app messaging: candidate ↔ employer conversation per application ---
v1.MapGet("/applications/{id:guid}/messages", async (
        Guid id,
        IQueryHandler<GetApplicationThreadQuery, IReadOnlyList<MessageDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetApplicationThreadQuery(id), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization()
    .WithName("GetApplicationThread")
    .WithSummary("List an application's candidate↔employer conversation. Requires a signed-in user.")
    .Produces<IReadOnlyList<MessageDto>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status401Unauthorized);

v1.MapPost("/applications/{id:guid}/messages", async (
        Guid id,
        SendMessageBody body,
        ICommandHandler<SendApplicationMessageCommand, MessageDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new SendApplicationMessageCommand(id, body.Sender, body.SenderName, body.Body), ct);
        return result.ToCreatedResult(dto => $"/v1/recruitment/applications/{id}/messages/{dto.Id}");
    })
    .RequireAuthorization()
    .WithName("SendApplicationMessage")
    .WithSummary("Post a message to an application conversation. Requires a signed-in user.")
    .Produces<MessageDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/applications/{id:guid}/messages/read", async (
        Guid id,
        MarkReadBody body,
        ICommandHandler<MarkThreadReadCommand, int> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new MarkThreadReadCommand(id, body.Reader), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization()
    .WithName("MarkThreadRead")
    .WithSummary("Mark the other side's messages in a conversation as read. Requires a signed-in user.")
    .Produces<int>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized);

// --- Bulk pipeline actions (advance/reject many applications at once) ---
v1.MapPost("/applications/bulk", async (
        BulkApplicationsBody body,
        ICommandHandler<BulkTransitionApplicationsCommand, BulkTransitionResultDto> handler,
        CancellationToken ct) =>
    {
        var action = string.Equals(body.Action, "reject", StringComparison.OrdinalIgnoreCase)
            ? ApplicationBulkAction.Reject
            : ApplicationBulkAction.Advance;
        var result = await handler.HandleAsync(new BulkTransitionApplicationsCommand(body.ApplicationIds ?? [], action), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("BulkTransitionApplications")
    .WithSummary("Advance or reject many applications at once. Requires an admin (write) role.")
    .Produces<BulkTransitionResultDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

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

/// <summary>Request body for setting a requisition's enrichment detail.</summary>
/// <param name="SalaryMin">Lower salary bound.</param>
/// <param name="SalaryMax">Upper salary bound.</param>
/// <param name="Currency">Currency code.</param>
/// <param name="EmploymentType">Employment-type name (fulltime/parttime/contract/internship/temporary).</param>
/// <param name="Remote">Whether remote.</param>
internal sealed record RequisitionDetailBody(int? SalaryMin, int? SalaryMax, string? Currency, string? EmploymentType, bool Remote);

/// <summary>Request body for adding a requisition tag.</summary>
/// <param name="Label">The tag label.</param>
internal sealed record RequisitionTagBody(string Label);

/// <summary>Request body for approving a requisition.</summary>
/// <param name="Approver">The approver's name.</param>
internal sealed record ApprovalDecisionBody(string Approver);

/// <summary>Request body for rejecting a requisition.</summary>
/// <param name="Approver">The approver's name.</param>
/// <param name="Reason">The rejection reason.</param>
internal sealed record ApprovalRejectBody(string Approver, string Reason);

/// <summary>Request body for creating a job template.</summary>
/// <param name="Name">Template name.</param>
/// <param name="Title">Default role title.</param>
/// <param name="City">Default city.</param>
/// <param name="Positions">Default positions.</param>
/// <param name="SalaryMin">Default lower salary bound.</param>
/// <param name="SalaryMax">Default upper salary bound.</param>
/// <param name="Currency">Default currency.</param>
/// <param name="EmploymentType">Default employment type.</param>
/// <param name="Remote">Default remote flag.</param>
/// <param name="Tags">Default tags.</param>
internal sealed record JobTemplateBody(string Name, string Title, string? City, int Positions, int? SalaryMin, int? SalaryMax, string? Currency, string? EmploymentType, bool Remote, IReadOnlyList<string>? Tags);

/// <summary>Request body for applying a template.</summary>
/// <param name="CompanyId">Hiring company id.</param>
internal sealed record UseTemplateBody(Guid CompanyId);

/// <summary>Request body for creating an email campaign.</summary>
/// <param name="Name">Internal name.</param>
/// <param name="Subject">Email subject.</param>
/// <param name="Body">Email body.</param>
internal sealed record CampaignBody(string Name, string Subject, string Body);

/// <summary>Request body for adding a campaign recipient.</summary>
/// <param name="Email">Recipient email.</param>
internal sealed record RecipientBody(string Email);

/// <summary>Request body for sending an application message.</summary>
/// <param name="Sender">Sender side (recruiter/talent).</param>
/// <param name="SenderName">Sender display name.</param>
/// <param name="Body">Message body.</param>
internal sealed record SendMessageBody(string? Sender, string SenderName, string Body);

/// <summary>Request body for marking a conversation read.</summary>
/// <param name="Reader">The reading side (recruiter/talent).</param>
internal sealed record MarkReadBody(string? Reader);

/// <summary>Request body for adding an interview attendee.</summary>
/// <param name="Name">Attendee name.</param>
/// <param name="Email">Attendee email.</param>
/// <param name="Role">Panel role.</param>
internal sealed record AttendeeBody(string Name, string? Email, string? Role);

/// <summary>Request body for a bulk pipeline action.</summary>
/// <param name="ApplicationIds">The applications to transition.</param>
/// <param name="Action">The action (<c>advance</c>/<c>reject</c>).</param>
internal sealed record BulkApplicationsBody(IReadOnlyList<Guid>? ApplicationIds, string Action);

/// <summary>Request body for starting an onboarding checklist.</summary>
/// <param name="RoleTitle">The hired role title.</param>
internal sealed record StartOnboardingBody(string RoleTitle);

/// <summary>Request body for toggling an onboarding task.</summary>
/// <param name="Done">Whether the task is complete.</param>
internal sealed record ToggleTaskBody(bool Done);

/// <summary>Request body for adding a custom onboarding task.</summary>
/// <param name="Label">The task label.</param>
internal sealed record AddTaskBody(string Label);

/// <summary>Request body for e-signing an offer.</summary>
/// <param name="SignerName">The name the candidate types as their signature.</param>
internal sealed record SignOfferBody(string SignerName);

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
