using Illumin360.Students.Application.Abstractions;
using Illumin360.Students.Application.Students;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Students.Application;

/// <summary>Registers Application-layer use-case handlers.</summary>
public static class DependencyInjection
{
    /// <summary>Adds CQRS handlers for the Students context.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddStudentsApplication(this IServiceCollection services)
    {
        services.AddScoped<
            IQueryHandler<GetStudentDashboardQuery, StudentDashboardDto>,
            GetStudentDashboardQueryHandler>();
        services.AddScoped<
            ICommandHandler<RegisterStudentCommand, StudentSummaryDto>,
            RegisterStudentCommandHandler>();
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
            ICommandHandler<ApplyCvSkillsCommand, AppliedSkillsDto>,
            ApplyCvSkillsCommandHandler>();
        return services;
    }
}
