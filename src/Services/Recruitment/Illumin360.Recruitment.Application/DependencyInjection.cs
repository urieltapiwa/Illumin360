using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Recruitment.Application;

/// <summary>Registers Application-layer use-case handlers.</summary>
public static class DependencyInjection
{
    /// <summary>Adds CQRS handlers for the Recruitment context.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddRecruitmentApplication(this IServiceCollection services)
    {
        services.AddScoped<
            IQueryHandler<GetRecruitmentRequestsQuery, IReadOnlyList<RecruitmentRequestDto>>,
            GetRecruitmentRequestsQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetRecruitmentRequestByIdQuery, RecruitmentRequestDto>,
            GetRecruitmentRequestByIdQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetApplicationsForRequestQuery, IReadOnlyList<ApplicationDto>>,
            GetApplicationsForRequestQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetRecruitmentStatsQuery, RecruitmentStatsDto>,
            GetRecruitmentStatsQueryHandler>();
        services.AddScoped<
            ICommandHandler<PostRecruitmentRequestCommand, RecruitmentRequestDto>,
            PostRecruitmentRequestCommandHandler>();
        services.AddScoped<
            ICommandHandler<ApplyToRequestCommand, ApplicationDto>,
            ApplyToRequestCommandHandler>();
        services.AddScoped<
            ICommandHandler<AdvanceApplicationCommand, ApplicationDto>,
            AdvanceApplicationCommandHandler>();
        services.AddScoped<
            ICommandHandler<RejectApplicationCommand, ApplicationDto>,
            RejectApplicationCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetTalentApplicationsQuery, IReadOnlyList<TalentApplicationDto>>,
            GetTalentApplicationsQueryHandler>();
        services.AddScoped<
            ICommandHandler<CreateSavedSearchCommand, SavedSearchDto>,
            CreateSavedSearchCommandHandler>();
        services.AddScoped<
            ICommandHandler<DeleteSavedSearchCommand, bool>,
            DeleteSavedSearchCommandHandler>();
        services.AddScoped<
            ICommandHandler<ToggleSavedSearchAlertsCommand, SavedSearchDto>,
            ToggleSavedSearchAlertsCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetSavedSearchesQuery, IReadOnlyList<SavedSearchDto>>,
            GetSavedSearchesQueryHandler>();
        services.AddScoped<
            IQueryHandler<RunSavedSearchQuery, IReadOnlyList<RecruitmentRequestDto>>,
            RunSavedSearchQueryHandler>();
        services.AddScoped<JobAlertRunner>();
        services.AddScoped<
            ICommandHandler<ScheduleInterviewCommand, InterviewDto>,
            ScheduleInterviewCommandHandler>();
        services.AddScoped<
            ICommandHandler<RecordInterviewFeedbackCommand, InterviewDto>,
            RecordInterviewFeedbackCommandHandler>();
        services.AddScoped<
            ICommandHandler<CancelInterviewCommand, InterviewDto>,
            CancelInterviewCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetInterviewsQuery, IReadOnlyList<InterviewDto>>,
            GetInterviewsQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetInterviewIcsQuery, string>,
            GetInterviewIcsQueryHandler>();
        return services;
    }
}
