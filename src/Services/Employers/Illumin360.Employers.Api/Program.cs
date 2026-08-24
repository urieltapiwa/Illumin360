using Illumin360.Employers.Application;
using Illumin360.Employers.Application.Abstractions;
using Illumin360.Employers.Application.Employers;
using Illumin360.Employers.Infrastructure;
using Illumin360.Employers.Infrastructure.Persistence;
using Illumin360.Observability;
using Illumin360.Security;
using Illumin360.Web;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Cross-cutting: OpenTelemetry + Serilog (charter Part 10) ---
builder.AddProjectObservability("employers");

// --- Liveness probe ---
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"]);

// --- Clean Architecture layers ---
builder.Services.AddEmployersApplication();
builder.Services.AddEmployersInfrastructure(builder.Configuration);

// --- AuthN/AuthZ (Keycloak JWTs relayed by the BFF) ---
builder.Services.AddIllumin360Auth(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply EF Core migrations on startup, then seed the demo employer if the database is empty.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EmployersDbContext>();
    await db.Database.MigrateAsync();
    await EmployersSeeder.SeedAsync(db, CancellationToken.None);
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();

app.MapProjectHealthChecks();

var v1 = app.MapGroup("/v1/employers").WithTags("Employers");

v1.MapGet("/me", async (
        IQueryHandler<GetEmployerQuery, EmployerDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetEmployerQuery(), ct);
        return result.ToHttpResult();
    })
    .WithName("GetMyEmployer")
    .WithSummary("Company profile for the current (demo) employer.")
    .Produces<EmployerDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapGet("/{id:guid}", async (
        Guid id,
        IQueryHandler<GetEmployerQuery, EmployerDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetEmployerQuery(id), ct);
        return result.ToHttpResult();
    })
    .WithName("GetEmployerById")
    .WithSummary("Company profile by id.")
    .Produces<EmployerDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/", async (
        RegisterEmployerCommand command,
        ICommandHandler<RegisterEmployerCommand, EmployerDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToCreatedResult(dto => $"/v1/employers/{dto.Id}");
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("RegisterEmployer")
    .WithSummary("Register a new employer company profile. Requires an admin (write) role.")
    .Produces<EmployerDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden);

v1.MapPut("/me", async (
        UpdateEmployerProfileCommand command,
        ICommandHandler<UpdateEmployerProfileCommand, EmployerDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.EmployerPolicy)
    .WithName("UpdateEmployerProfile")
    .WithSummary("Update the current employer's profile. Requires an admin (write) role.")
    .Produces<EmployerDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound);

// --- Team members (multi-user employer accounts with owner/recruiter/viewer roles) ---
var team = v1.MapGroup("/me/team").WithTags("Employer team");

team.MapGet("/", async (
        IQueryHandler<ListTeamMembersQuery, IReadOnlyList<TeamMemberDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new ListTeamMembersQuery(), ct);
        return result.ToHttpResult();
    })
    .WithName("ListTeamMembers")
    .WithSummary("List the current employer's team members.")
    .Produces<IReadOnlyList<TeamMemberDto>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

team.MapPost("/", async (
        InviteTeamMemberCommand command,
        ICommandHandler<InviteTeamMemberCommand, TeamMemberDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToCreatedResult(dto => $"/v1/employers/me/team/{dto.Id}");
    })
    .RequireAuthorization(AuthenticationExtensions.EmployerPolicy)
    .WithName("InviteTeamMember")
    .WithSummary("Invite a new team member (owner/recruiter/viewer). Requires an admin (write) role.")
    .Produces<TeamMemberDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status409Conflict);

team.MapPut("/{memberId:guid}/role", async (
        Guid memberId,
        ChangeRoleRequest body,
        ICommandHandler<ChangeTeamMemberRoleCommand, TeamMemberDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new ChangeTeamMemberRoleCommand(memberId, body.Role), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.EmployerPolicy)
    .WithName("ChangeTeamMemberRole")
    .WithSummary("Change a team member's role. Requires an admin (write) role.")
    .Produces<TeamMemberDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

team.MapDelete("/{memberId:guid}", async (
        Guid memberId,
        ICommandHandler<RemoveTeamMemberCommand, bool> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new RemoveTeamMemberCommand(memberId), ct);
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.EmployerPolicy)
    .WithName("RemoveTeamMember")
    .WithSummary("Remove a team member. Requires an admin (write) role.")
    .Produces(StatusCodes.Status204NoContent)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status403Forbidden)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

app.Run();

/// <summary>Exposed so integration tests can use <c>WebApplicationFactory</c> (charter Part 14).</summary>
public partial class Program;
