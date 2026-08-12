using Illumin360.Billing.Application.Abstractions;
using Illumin360.Billing.Application.Billing;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Billing.Application;

/// <summary>Registers Application-layer use-case handlers for the Billing context.</summary>
public static class DependencyInjection
{
    /// <summary>Adds CQRS handlers + the recurring-billing runner.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddBillingApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreatePlanCommand, PlanDto>, CreatePlanCommandHandler>();
        services.AddScoped<IQueryHandler<ListPlansQuery, IReadOnlyList<PlanDto>>, ListPlansQueryHandler>();
        services.AddScoped<ICommandHandler<SubscribeCommand, SubscriptionDto>, SubscribeCommandHandler>();
        services.AddScoped<ICommandHandler<CancelSubscriptionCommand, SubscriptionDto>, CancelSubscriptionCommandHandler>();
        services.AddScoped<IQueryHandler<GetSubscriptionQuery, SubscriptionDto>, GetSubscriptionQueryHandler>();
        services.AddScoped<IQueryHandler<ListInvoicesQuery, IReadOnlyList<InvoiceDto>>, ListInvoicesQueryHandler>();
        services.AddScoped<IQueryHandler<GetEntitlementsQuery, EntitlementsDto>, GetEntitlementsQueryHandler>();
        services.AddScoped<BillingRunner>();
        return services;
    }
}
