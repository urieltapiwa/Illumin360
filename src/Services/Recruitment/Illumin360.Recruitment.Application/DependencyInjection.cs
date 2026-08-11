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
            IQueryHandler<GetHiringMetricsQuery, HiringMetricsDto>,
            GetHiringMetricsQueryHandler>();
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
            IQueryHandler<GetTalentCalendarFeedQuery, string>,
            GetTalentCalendarFeedQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetInterviewAttendeesQuery, IReadOnlyList<InterviewAttendeeDto>>,
            GetInterviewAttendeesQueryHandler>();
        services.AddScoped<
            ICommandHandler<AddInterviewAttendeeCommand, InterviewAttendeeDto>,
            AddInterviewAttendeeCommandHandler>();
        services.AddScoped<
            ICommandHandler<RemoveInterviewAttendeeCommand, bool>,
            RemoveInterviewAttendeeCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetFormQuestionsQuery, IReadOnlyList<FormQuestionDto>>,
            GetFormQuestionsQueryHandler>();
        services.AddScoped<
            ICommandHandler<AddFormQuestionCommand, FormQuestionDto>,
            AddFormQuestionCommandHandler>();
        services.AddScoped<
            ICommandHandler<RemoveFormQuestionCommand, bool>,
            RemoveFormQuestionCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetApplicationAnswersQuery, IReadOnlyList<AnswerDto>>,
            GetApplicationAnswersQueryHandler>();
        services.AddScoped<
            ICommandHandler<SubmitApplicationAnswersCommand, int>,
            SubmitApplicationAnswersCommandHandler>();
        services.AddScoped<
            ICommandHandler<SetRequisitionInternalCommand, RequisitionDetailDto>,
            SetRequisitionInternalCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetReferralsQuery, IReadOnlyList<ReferralDto>>,
            GetReferralsQueryHandler>();
        services.AddScoped<
            ICommandHandler<SubmitReferralCommand, ReferralDto>,
            SubmitReferralCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetApplicationSourceQuery, ApplicationSourceDto>,
            GetApplicationSourceQueryHandler>();
        services.AddScoped<
            ICommandHandler<SetApplicationSourceCommand, ApplicationSourceDto>,
            SetApplicationSourceCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetChannelBreakdownQuery, IReadOnlyList<SourceMetric>>,
            GetChannelBreakdownQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetSkillRatingsQuery, IReadOnlyList<SkillRatingDto>>,
            GetSkillRatingsQueryHandler>();
        services.AddScoped<
            ICommandHandler<RecordSkillRatingsCommand, IReadOnlyList<SkillRatingDto>>,
            RecordSkillRatingsCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetInterviewSummaryQuery, InterviewSummaryDto>,
            GetInterviewSummaryQueryHandler>();
        services.AddScoped<
            ICommandHandler<RecordCareerViewCommand, bool>,
            RecordCareerViewCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetCareerViewsQuery, IReadOnlyList<CareerViewDto>>,
            GetCareerViewsQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetApplicationThreadQuery, IReadOnlyList<MessageDto>>,
            GetApplicationThreadQueryHandler>();
        services.AddScoped<
            ICommandHandler<SendApplicationMessageCommand, MessageDto>,
            SendApplicationMessageCommandHandler>();
        services.AddScoped<
            ICommandHandler<MarkThreadReadCommand, int>,
            MarkThreadReadCommandHandler>();
        services.AddScoped<
            IQueryHandler<GetCampaignsQuery, IReadOnlyList<CampaignDto>>,
            GetCampaignsQueryHandler>();
        services.AddScoped<
            IQueryHandler<GetCampaignQuery, CampaignDto>,
            GetCampaignQueryHandler>();
        services.AddScoped<
            ICommandHandler<CreateCampaignCommand, CampaignDto>,
            CreateCampaignCommandHandler>();
        services.AddScoped<
            ICommandHandler<AddCampaignRecipientCommand, CampaignDto>,
            AddCampaignRecipientCommandHandler>();
        services.AddScoped<
            ICommandHandler<RemoveCampaignRecipientCommand, CampaignDto>,
            RemoveCampaignRecipientCommandHandler>();
        services.AddScoped<
            ICommandHandler<SendCampaignCommand, CampaignDto>,
            SendCampaignCommandHandler>();
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
