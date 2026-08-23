using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Application.Accounts;
using Illumin360.Admin.Application.Audit;
using Illumin360.Admin.Application.Dashboard;
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
        services.AddScoped<
            IQueryHandler<GetAuditLogQuery, IReadOnlyList<AuditEntryDto>>,
            GetAuditLogQueryHandler>();

        services.AddScoped<
            IQueryHandler<GetAdminSummaryQuery, AdminSummaryDto>,
            GetAdminSummaryQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetTalentInsightsQuery, TalentInsightsDto>,
            GetTalentInsightsQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetSupportSummaryQuery, SupportSummaryDto>,
            GetSupportSummaryQueryHandler>();

        return services;
    }
}
