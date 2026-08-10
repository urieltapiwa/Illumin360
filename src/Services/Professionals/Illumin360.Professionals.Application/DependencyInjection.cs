using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Application.Professionals;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Professionals.Application;

/// <summary>Registers Application-layer use-case handlers.</summary>
public static class DependencyInjection
{
    /// <summary>Adds CQRS handlers for the Professionals context.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddProfessionalsApplication(this IServiceCollection services)
    {
        services.AddScoped<
            IQueryHandler<GetProfessionalDashboardQuery, ProfessionalDashboardDto>,
            GetProfessionalDashboardQueryHandler>();
        services.AddScoped<
            ICommandHandler<RegisterProfessionalCommand, ProfessionalSummaryDto>,
            RegisterProfessionalCommandHandler>();
        services.AddScoped<
            ICommandHandler<UpdateMatchStatusCommand, MatchDto>,
            UpdateMatchStatusCommandHandler>();
        services.AddScoped<
            ICommandHandler<SetAvailabilityCommand, string>,
            SetAvailabilityCommandHandler>();
        services.AddScoped<
            ICommandHandler<UploadCvCommand, CvDto>,
            UploadCvCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetCvMetadataQuery, CvDto>,
            GetCvMetadataQueryHandler>();
        services.AddScoped<
            IQueryHandler<DownloadCvQuery, CvContent>,
            DownloadCvQueryHandler>();
        services.AddScoped<
            IQueryHandler<ScoreRolesQuery, IReadOnlyList<RoleScoreDto>>,
            ScoreRolesQueryHandler>();
        return services;
    }
}
