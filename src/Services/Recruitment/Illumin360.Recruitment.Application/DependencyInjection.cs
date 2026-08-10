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
        services.AddScoped<
            IQueryHandler<ListClientsQuery, IReadOnlyList<ClientDto>>,
            ListClientsQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetClientQuery, ClientDetailDto>,
            GetClientQueryHandler>();
        services.AddScoped<
            ICommandHandler<CreateClientCommand, ClientDto>,
            CreateClientCommandHandler>();
        services.AddScoped<
            ICommandHandler<ChangeClientStatusCommand, ClientDto>,
            ChangeClientStatusCommandHandler>();
        services.AddScoped<
            ICommandHandler<AddClientContactCommand, ClientContactDto>,
            AddClientContactCommandHandler>();
        services.AddScoped<
            ICommandHandler<RemoveClientContactCommand, bool>,
            RemoveClientContactCommandHandler>();
        services.AddScoped<
            ICommandHandler<CreateOfferCommand, OfferDto>,
            CreateOfferCommandHandler>();
        services.AddScoped<
            ICommandHandler<TransitionOfferCommand, OfferDto>,
            TransitionOfferCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetOffersQuery, IReadOnlyList<OfferDto>>,
            GetOffersQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetOfferQuery, OfferDto>,
            GetOfferQueryHandler>();
        services.AddScoped<
            ICommandHandler<SignOfferCommand, OfferDto>,
            SignOfferCommandHandler>();
        services.AddScoped<
            ICommandHandler<StartOnboardingCommand, OnboardingChecklistDto>,
            StartOnboardingCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetOnboardingQuery, OnboardingChecklistDto>,
            GetOnboardingQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetRequisitionDetailQuery, RequisitionDetailDto>,
            GetRequisitionDetailQueryHandler>();
        services.AddScoped<
            ICommandHandler<SetRequisitionDetailCommand, RequisitionDetailDto>,
            SetRequisitionDetailCommandHandler>();
        services.AddScoped<
            ICommandHandler<AddRequisitionTagCommand, IReadOnlyList<string>>,
            AddRequisitionTagCommandHandler>();
        services.AddScoped<
            ICommandHandler<RemoveRequisitionTagCommand, IReadOnlyList<string>>,
            RemoveRequisitionTagCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetApprovalQuery, ApprovalDto>,
            GetApprovalQueryHandler>();
        services.AddScoped<
            ICommandHandler<TransitionApprovalCommand, ApprovalDto>,
            TransitionApprovalCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetJobTemplatesQuery, IReadOnlyList<JobTemplateDto>>,
            GetJobTemplatesQueryHandler>();
        services.AddScoped<
            ICommandHandler<CreateJobTemplateCommand, JobTemplateDto>,
            CreateJobTemplateCommandHandler>();
        services.AddScoped<
            ICommandHandler<DeleteJobTemplateCommand, bool>,
            DeleteJobTemplateCommandHandler>();
        services.AddScoped<
            ICommandHandler<UseJobTemplateCommand, RecruitmentRequestDto>,
            UseJobTemplateCommandHandler>();
        services.AddScoped<
            ICommandHandler<BulkTransitionApplicationsCommand, BulkTransitionResultDto>,
            BulkTransitionApplicationsCommandHandler>();
        services.AddScoped<
            ICommandHandler<ToggleOnboardingTaskCommand, OnboardingTaskDto>,
            ToggleOnboardingTaskCommandHandler>();
        services.AddScoped<
            ICommandHandler<AddOnboardingTaskCommand, OnboardingTaskDto>,
            AddOnboardingTaskCommandHandler>();
        services.AddScoped<
            ICommandHandler<RemoveOnboardingTaskCommand, bool>,
            RemoveOnboardingTaskCommandHandler>();
        return services;
    }
}
