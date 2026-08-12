using Illumin360.Payments.Application.Abstractions;
using Illumin360.Payments.Application.Payments;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Payments.Application;

/// <summary>Registers Application-layer use-case handlers for the Payments context.</summary>
public static class DependencyInjection
{
    /// <summary>Adds CQRS handlers.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddPaymentsApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateContractCommand, ContractDto>, CreateContractCommandHandler>();
        services.AddScoped<ICommandHandler<AddMilestoneCommand, MilestoneDto>, AddMilestoneCommandHandler>();
        services.AddScoped<ICommandHandler<ActivateContractCommand, ContractDto>, ActivateContractCommandHandler>();
        services.AddScoped<ICommandHandler<CancelContractCommand, ContractDto>, CancelContractCommandHandler>();
        services.AddScoped<ICommandHandler<FundMilestoneCommand, MilestoneDto>, FundMilestoneCommandHandler>();
        services.AddScoped<ICommandHandler<SubmitMilestoneCommand, MilestoneDto>, SubmitMilestoneCommandHandler>();
        services.AddScoped<ICommandHandler<ApproveMilestoneCommand, MilestoneDto>, ApproveMilestoneCommandHandler>();
        services.AddScoped<ICommandHandler<RefundMilestoneCommand, MilestoneDto>, RefundMilestoneCommandHandler>();
        services.AddScoped<IQueryHandler<ListContractsQuery, IReadOnlyList<ContractDto>>, ListContractsQueryHandler>();
        services.AddScoped<IQueryHandler<GetContractQuery, ContractDetailDto>, GetContractQueryHandler>();
        services.AddScoped<ICommandHandler<RegisterPayoutAccountCommand, PayoutAccountDto>, RegisterPayoutAccountCommandHandler>();
        services.AddScoped<ICommandHandler<VerifyPayoutAccountCommand, PayoutAccountDto>, VerifyPayoutAccountCommandHandler>();
        services.AddScoped<IQueryHandler<GetPayoutAccountQuery, PayoutAccountDto>, GetPayoutAccountQueryHandler>();
        return services;
    }
}
