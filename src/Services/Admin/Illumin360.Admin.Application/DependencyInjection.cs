using Illumin360.Admin.Application.Abstractions;
using Illumin360.Admin.Application.Verifications;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Admin.Application;

/// <summary>Registers Application-layer use-case handlers for the Admin service.</summary>
public static class DependencyInjection
{
    /// <summary>Adds CQRS handlers for the Admin context.</summary>
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
        return services;
    }
}
