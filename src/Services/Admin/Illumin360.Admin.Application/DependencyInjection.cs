using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Application.Accounts;
using Illumin360.Admin.Application.Tickets;
using Illumin360.Admin.Application.Verifications;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Admin.Application;

/// <summary>Registers Application-layer use-case handlers for the Admin service.</summary>
public static class DependencyInjection
{
    /// <summary>Adds CQRS handlers for the Admin context (verifications, tickets, accounts).</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddAdminApplication(this IServiceCollection services)
    {
        services.AddScoped<
            IQueryHandler<GetVerificationsQuery, IReadOnlyList<VerificationDto>>,
            GetVerificationsQueryHandler>();
        services.AddScoped<
            ICommandHandler<DecideVerificationCommand, VerificationDto>,
            DecideVerificationCommandHandler>();

        services.AddScoped<
            IQueryHandler<GetTicketsQuery, IReadOnlyList<TicketDto>>,
            GetTicketsQueryHandler>();
        services.AddScoped<
            ICommandHandler<TriageTicketCommand, TicketDto>,
            TriageTicketCommandHandler>();

        services.AddScoped<
            IQueryHandler<GetAccountsQuery, IReadOnlyList<AccountDto>>,
            GetAccountsQueryHandler>();
        services.AddScoped<
            ICommandHandler<SetAccountStatusCommand, AccountDto>,
            SetAccountStatusCommandHandler>();

        return services;
    }
}
