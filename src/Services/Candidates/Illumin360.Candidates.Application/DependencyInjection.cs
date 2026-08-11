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
        services.AddScoped<IQueryHandler<SearchCandidatesQuery, CandidateSearchResultDto>, SearchCandidatesQueryHandler>();
        services.AddScoped<IQueryHandler<FindDuplicateCandidatesQuery, IReadOnlyList<DuplicateGroupDto>>, FindDuplicateCandidatesQueryHandler>();
        services.AddScoped<IQueryHandler<GetCandidateExportQuery, CandidateExportDto>, GetCandidateExportQueryHandler>();
        services.AddScoped<ICommandHandler<EraseCandidateCommand, bool>, EraseCandidateCommandHandler>();
        services.AddScoped<IQueryHandler<GetDiversityReportQuery, DiversityReportDto>, GetDiversityReportQueryHandler>();
        services.AddScoped<IQueryHandler<GetCandidateNotesQuery, IReadOnlyList<CandidateNoteDto>>, GetCandidateNotesQueryHandler>();
        services.AddScoped<ICommandHandler<AddCandidateNoteCommand, CandidateNoteDto>, AddCandidateNoteCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveCandidateNoteCommand, bool>, RemoveCandidateNoteCommandHandler>();
        services.AddScoped<IQueryHandler<GetCandidateTagsQuery, IReadOnlyList<string>>, GetCandidateTagsQueryHandler>();
        services.AddScoped<ICommandHandler<AddCandidateTagCommand, IReadOnlyList<string>>, AddCandidateTagCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveCandidateTagCommand, IReadOnlyList<string>>, RemoveCandidateTagCommandHandler>();
        services.AddScoped<IQueryHandler<GetCandidateByIdQuery, CandidateDto>, GetCandidateByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetCandidateStatsQuery, CandidateStatsDto>, GetCandidateStatsQueryHandler>();
        services.AddScoped<ICommandHandler<RegisterCandidateCommand, CandidateDto>, RegisterCandidateCommandHandler>();
        services.AddScoped<ICommandHandler<UploadCandidateCvCommand, CvDto>, UploadCandidateCvCommandHandler>();
        services.AddScoped<IQueryHandler<GetCandidateCvMetadataQuery, CvDto>, GetCandidateCvMetadataQueryHandler>();
        services.AddScoped<IQueryHandler<DownloadCandidateCvQuery, CvContent>, DownloadCandidateCvQueryHandler>();
        services.AddScoped<IQueryHandler<GetTopCandidatesQuery, IReadOnlyList<RankedCandidateDto>>, GetTopCandidatesQueryHandler>();
        services.AddScoped<ICommandHandler<CreateTalentPoolCommand, TalentPoolDto>, CreateTalentPoolCommandHandler>();
        services.AddScoped<ICommandHandler<AddToPoolCommand, bool>, AddToPoolCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveFromPoolCommand, bool>, RemoveFromPoolCommandHandler>();
        services.AddScoped<IQueryHandler<GetPoolsQuery, IReadOnlyList<TalentPoolDto>>, GetPoolsQueryHandler>();
        services.AddScoped<IQueryHandler<GetPoolMembersQuery, IReadOnlyList<PoolMemberDto>>, GetPoolMembersQueryHandler>();
        return services;
    }
}
