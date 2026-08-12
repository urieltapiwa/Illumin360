using Illumin360.Payments.Application;
using Illumin360.Payments.Application.Abstractions;
using Illumin360.Payments.Application.Payments;
using Illumin360.Payments.Infrastructure;
using Illumin360.Payments.Infrastructure.Persistence;
using Illumin360.Observability;
using Illumin360.Security;
using Illumin360.Web;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Cross-cutting: OpenTelemetry + Serilog (charter Part 10) ---
builder.AddProjectObservability("payments");

// --- Liveness probe ---
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"]);

// --- Clean Architecture layers ---
builder.Services.AddPaymentsApplication();
builder.Services.AddPaymentsInfrastructure(builder.Configuration);

// --- Payment provider (decision D1) ---
// Default = Fake (Phase 1, no real money). A real PSP adapter is used ONLY when Payments:Provider names one,
// Enabled=true, and a BaseUrl is set — and going live still requires the D2 legal sign-off + credentials.
var paymentOptions = builder.Configuration.GetSection("Payments").Get<Illumin360.Payments.Infrastructure.Providers.PaymentProviderOptions>()
    ?? new Illumin360.Payments.Infrastructure.Providers.PaymentProviderOptions();
builder.Services.AddSingleton(paymentOptions);
switch (paymentOptions.UseReal ? paymentOptions.Provider : Illumin360.Payments.Infrastructure.Providers.PaymentProviderKind.Fake)
{
    case Illumin360.Payments.Infrastructure.Providers.PaymentProviderKind.Flutterwave:
        builder.Services.AddHttpClient<Illumin360.Payments.Application.Abstractions.IPaymentProvider, Illumin360.Payments.Infrastructure.Providers.FlutterwavePaymentProvider>();
        break;
    case Illumin360.Payments.Infrastructure.Providers.PaymentProviderKind.Stripe:
        builder.Services.AddHttpClient<Illumin360.Payments.Application.Abstractions.IPaymentProvider, Illumin360.Payments.Infrastructure.Providers.StripeConnectPaymentProvider>();
        break;
    case Illumin360.Payments.Infrastructure.Providers.PaymentProviderKind.NGenius:
        builder.Services.AddHttpClient<Illumin360.Payments.Application.Abstractions.IPaymentProvider, Illumin360.Payments.Infrastructure.Providers.NGeniusPaymentProvider>();
        break;
    case Illumin360.Payments.Infrastructure.Providers.PaymentProviderKind.Dpo:
        builder.Services.AddHttpClient<Illumin360.Payments.Application.Abstractions.IPaymentProvider, Illumin360.Payments.Infrastructure.Providers.DpoPaymentProvider>();
        break;
    default:
        builder.Services.AddSingleton<Illumin360.Payments.Application.Abstractions.IPaymentProvider, Illumin360.Payments.Infrastructure.FakePaymentProvider>();
        break;
}

// --- AuthN/AuthZ (Keycloak JWTs relayed by the BFF) ---
builder.Services.AddIllumin360Auth(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply EF Core migrations on startup.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();

app.MapProjectHealthChecks();

var v1 = app.MapGroup("/v1/payments").WithTags("Payments");

v1.MapGet("/contracts", async (
        Guid? clientId,
        Guid? talentId,
        IQueryHandler<ListContractsQuery, IReadOnlyList<ContractDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new ListContractsQuery(clientId, talentId), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization()
    .WithName("ListContracts")
    .WithSummary("List contracts, optionally filtered by client or talent.")
    .Produces<IReadOnlyList<ContractDto>>(StatusCodes.Status200OK);

v1.MapGet("/contracts/{id:guid}", async (
        Guid id,
        IQueryHandler<GetContractQuery, ContractDetailDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetContractQuery(id), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization()
    .WithName("GetContract")
    .WithSummary("Get a contract with its milestones and ledger movements.")
    .Produces<ContractDetailDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/contracts", async (
        CreateContractCommand command,
        ICommandHandler<CreateContractCommand, ContractDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToCreatedResult(dto => $"/v1/payments/contracts/{dto.Id}");
    })
    .RequireAuthorization()
    .WithName("CreateContract")
    .WithSummary("Create a draft fixed-price contract.")
    .Produces<ContractDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest);

v1.MapPost("/contracts/{id:guid}/milestones", async (
        Guid id,
        AddMilestoneBody body,
        ICommandHandler<AddMilestoneCommand, MilestoneDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new AddMilestoneCommand(id, body.Title, body.AmountMinor), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization()
    .WithName("AddMilestone")
    .WithSummary("Add a milestone to a draft contract.")
    .Produces<MilestoneDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/contracts/{id:guid}/activate", async (
        Guid id,
        ICommandHandler<ActivateContractCommand, ContractDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new ActivateContractCommand(id), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization()
    .WithName("ActivateContract")
    .WithSummary("Activate a draft contract (requires at least one milestone).")
    .Produces<ContractDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/contracts/{id:guid}/cancel", async (
        Guid id,
        ICommandHandler<CancelContractCommand, ContractDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new CancelContractCommand(id), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization()
    .WithName("CancelContract")
    .WithSummary("Cancel a contract before completion.")
    .Produces<ContractDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

// --- Milestone money transitions (through the payment-provider port) ---
static RouteHandlerBuilder MapMilestoneAction<TCommand>(RouteGroupBuilder group, string verb, string name, string summary, Func<Guid, TCommand> make)
    where TCommand : ICommand<MilestoneDto>
    => group.MapPost($"/milestones/{{id:guid}}/{verb}", async (
            Guid id,
            ICommandHandler<TCommand, MilestoneDto> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(make(id), ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithName(name)
        .WithSummary(summary)
        .Produces<MilestoneDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

MapMilestoneAction(v1, "fund", "FundMilestone", "Fund a milestone (client → escrow) via the payment provider.", id => new FundMilestoneCommand(id));
MapMilestoneAction(v1, "submit", "SubmitMilestone", "Submit a funded milestone (talent).", id => new SubmitMilestoneCommand(id));
MapMilestoneAction(v1, "approve", "ApproveMilestone", "Approve a submitted milestone, releasing escrow to the talent.", id => new ApproveMilestoneCommand(id));
MapMilestoneAction(v1, "refund", "RefundMilestone", "Refund a funded/submitted milestone to the client.", id => new RefundMilestoneCommand(id));

app.Run();

/// <summary>Exposed so integration tests can use <c>WebApplicationFactory</c> (charter Part 14).</summary>
public partial class Program;

/// <summary>Request body for adding a milestone.</summary>
/// <param name="Title">Milestone title.</param>
/// <param name="AmountMinor">Amount in minor units.</param>
internal sealed record AddMilestoneBody(string Title, long AmountMinor);
