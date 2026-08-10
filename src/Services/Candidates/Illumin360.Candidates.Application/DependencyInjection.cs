using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Candidates.Application;

/// <summary>Registers Application-layer use-case handlers.</summary>
public static class DependencyInjection
{
    /// <summary>Adds CQRS handlers for the Candidates context.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddCandidatesApplication(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetCandidatesQuery, IReadOnlyList<CandidateDto>>, GetCandidatesQueryHandler>();
        services.AddScoped<IQueryHandler<GetCandidateByIdQuery, CandidateDto>, GetCandidateByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetCandidateStatsQuery, CandidateStatsDto>, GetCandidateStatsQueryHandler>();
        services.AddScoped<ICommandHandler<RegisterCandidateCommand, CandidateDto>, RegisterCandidateCommandHandler>();
        services.AddScoped<ICommandHandler<UploadCandidateCvCommand, CvDto>, UploadCandidateCvCommandHandler>();
        services.AddScoped<IQueryHandler<GetCandidateCvMetadataQuery, CvDto>, GetCandidateCvMetadataQueryHandler>();
        services.AddScoped<IQueryHandler<DownloadCandidateCvQuery, CvContent>, DownloadCandidateCvQueryHandler>();
        return services;
    }
}
