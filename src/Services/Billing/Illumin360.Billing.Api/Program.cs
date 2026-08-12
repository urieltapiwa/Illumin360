using Illumin360.Billing.Application;
using Illumin360.Billing.Application.Abstractions;
using Illumin360.Billing.Application.Billing;
using Illumin360.Billing.Infrastructure;
using Illumin360.Billing.Infrastructure.Persistence;
using Illumin360.Billing.Infrastructure.Providers;
using Illumin360.Observability;
using Illumin360.Security;
using Illumin360.Web;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddProjectObservability("billing");

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"]);

builder.Services.AddBillingApplication();
builder.Services.AddBillingInfrastructure(builder.Configuration);

// Recurring-billing provider — Fake by default (no real money). Real adapters (DPO/Flutterwave/N-Genius) are a
// follow-up; they register here as typed HttpClients when Billing:Provider opts in.
builder.Services.AddSingleton<IBillingProvider, FakeBillingProvider>();

// Charge due renewals on a timer (enabled by default; interval via Billing:IntervalSeconds).
if (builder.Configuration.GetValue<bool?>("Billing:Enabled") ?? true)
{
    builder.Services.AddHostedService<Illumin360.Billing.Api.BillingScheduler>();
}

builder.Services.AddIllumin360Auth(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();
app.MapProjectHealthChecks();

var v1 = app.MapGroup("/v1/billing").WithTags("Billing");

v1.MapGet("/plans", async (
        IQueryHandler<ListPlansQuery, IReadOnlyList<PlanDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new ListPlansQuery(), ct);
        return result.ToHttpResult();
    })
    .WithName("ListPlans")
    .WithSummary("List active subscription plans.")
    .Produces<IReadOnlyList<PlanDto>>(StatusCodes.Status200OK);

v1.MapPost("/plans", async (
        CreatePlanCommand command,
        ICommandHandler<CreatePlanCommand, PlanDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization(AuthenticationExtensions.AdminWritePolicy)
    .WithName("CreatePlan")
    .WithSummary("Create a subscription plan. Requires an admin-write role.")
    .Produces<PlanDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapPost("/subscriptions", async (
        SubscribeCommand command,
        ICommandHandler<SubscribeCommand, SubscriptionDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization()
    .WithName("Subscribe")
    .WithSummary("Subscribe a customer to a plan (by code).")
    .Produces<SubscriptionDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapGet("/subscriptions/{customerId:guid}", async (
        Guid customerId,
        IQueryHandler<GetSubscriptionQuery, SubscriptionDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetSubscriptionQuery(customerId), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization()
    .WithName("GetSubscription")
    .WithSummary("Get a customer's current subscription.")
    .Produces<SubscriptionDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

v1.MapPost("/subscriptions/{customerId:guid}/cancel", async (
        Guid customerId,
        ICommandHandler<CancelSubscriptionCommand, SubscriptionDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new CancelSubscriptionCommand(customerId), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization()
    .WithName("CancelSubscription")
    .WithSummary("Cancel a customer's subscription.")
    .Produces<SubscriptionDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

v1.MapGet("/subscriptions/{customerId:guid}/invoices", async (
        Guid customerId,
        IQueryHandler<ListInvoicesQuery, IReadOnlyList<InvoiceDto>> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new ListInvoicesQuery(customerId), ct);
        return result.ToHttpResult();
    })
    .RequireAuthorization()
    .WithName("ListInvoices")
    .WithSummary("List a customer's invoices.")
    .Produces<IReadOnlyList<InvoiceDto>>(StatusCodes.Status200OK);

v1.MapGet("/entitlements/{customerId:guid}", async (
        Guid customerId,
        IQueryHandler<GetEntitlementsQuery, EntitlementsDto> handler,
        CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(new GetEntitlementsQuery(customerId), ct);
        return result.ToHttpResult();
    })
    .WithName("GetEntitlements")
    .WithSummary("Get a customer's entitlements (the feature keys granted by their active plan).")
    .Produces<EntitlementsDto>(StatusCodes.Status200OK);

app.Run();

/// <summary>Exposed so integration tests can use <c>WebApplicationFactory</c> (charter Part 14).</summary>
public partial class Program;
