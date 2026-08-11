using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using ApplicationId = Illumin360.Recruitment.Domain.ApplicationId;

namespace Illumin360.Recruitment.Application.Abstractions;

/// <summary>
/// Port for recruitment persistence. Implemented by the Infrastructure layer
/// (ports &amp; adapters — charter Part 5).
/// </summary>
public interface IRecruitmentRepository
{
    /// <summary>Returns a page of recruitment requests, optionally filtered by city and/or status.</summary>
    /// <param name="city">Optional city filter (case-insensitive).</param>
    /// <param name="status">Optional status filter (<c>open</c>/<c>filled</c>/<c>closed</c>, case-insensitive).</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching requests.</returns>
    Task<IReadOnlyList<RecruitmentRequest>> ListAsync(
        string? city, string? status, int skip, int take, CancellationToken cancellationToken);

    /// <summary>Finds a request by id, or null if not present.</summary>
    /// <param name="id">The request id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RecruitmentRequest?> GetByIdAsync(RequestId id, CancellationToken cancellationToken);

    /// <summary>Returns a page of applications for a request, highest match score first.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<RecruitmentApplication>> ListApplicationsAsync(
        RequestId requestId, int skip, int take, CancellationToken cancellationToken);

    /// <summary>Stages a new request for insertion. Persisted on <see cref="SaveChangesAsync"/>.</summary>
    /// <param name="request">The request to add.</param>
    void Add(RecruitmentRequest request);

    /// <summary>Whether the given talent already has an application against the request.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="talentId">The talent id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> HasApplicationAsync(RequestId requestId, Guid talentId, CancellationToken cancellationToken);

    /// <summary>Stages a new application for insertion. Persisted on <see cref="SaveChangesAsync"/>.</summary>
    /// <param name="application">The application to add.</param>
    void AddApplication(RecruitmentApplication application);

    /// <summary>Loads an application for update (change-tracked), or null if not present.</summary>
    /// <param name="id">The application id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tracked application, or null.</returns>
    Task<RecruitmentApplication?> GetApplicationAsync(ApplicationId id, CancellationToken cancellationToken);

    /// <summary>Stages a new rejection record for insertion.</summary>
    /// <param name="rejection">The rejection to add.</param>
    void AddApplicationRejection(ApplicationRejection rejection);

    /// <summary>Loads an application's rejection record for update, or null if none.</summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ApplicationRejection?> GetRejectionForApplicationAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>Returns rejection reasons for the given application ids, keyed by application id.</summary>
    /// <param name="applicationIds">The application ids.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyDictionary<Guid, string>> GetRejectionReasonsAsync(IReadOnlyList<Guid> applicationIds, CancellationToken cancellationToken);

    /// <summary>Stages a new saved search for insertion.</summary>
    /// <param name="savedSearch">The saved search to add.</param>
    void AddSavedSearch(SavedSearch savedSearch);

    /// <summary>Removes a saved search.</summary>
    /// <param name="savedSearch">The saved search to remove.</param>
    void RemoveSavedSearch(SavedSearch savedSearch);

    /// <summary>Lists a talent's saved searches, most recent first.</summary>
    /// <param name="talentId">The talent id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<SavedSearch>> ListSavedSearchesForTalentAsync(Guid talentId, CancellationToken cancellationToken);

    /// <summary>Loads a saved search for update, or null if not present.</summary>
    /// <param name="id">The saved-search id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<SavedSearch?> GetSavedSearchAsync(SavedSearchId id, CancellationToken cancellationToken);

    /// <summary>Lists all saved searches with job alerts enabled (across talents).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<SavedSearch>> ListAlertEnabledSavedSearchesAsync(CancellationToken cancellationToken);

    /// <summary>Stages a new interview for insertion.</summary>
    /// <param name="interview">The interview to add.</param>
    void AddInterview(Interview interview);

    /// <summary>Loads an interview for update, or null if not present.</summary>
    /// <param name="id">The interview id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Interview?> GetInterviewAsync(InterviewId id, CancellationToken cancellationToken);

    /// <summary>Lists an application's interviews, soonest first.</summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Interview>> ListInterviewsForApplicationAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>Lists all interviews across a talent's applications, soonest first (for their calendar feed).</summary>
    /// <param name="talentId">The talent id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Interview>> ListInterviewsForTalentAsync(Guid talentId, CancellationToken cancellationToken);

    /// <summary>Lists a talent's applications, most recent first.</summary>
    /// <param name="talentId">The talent id.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The talent's applications.</returns>
    Task<IReadOnlyList<RecruitmentApplication>> ListApplicationsForTalentAsync(Guid talentId, int skip, int take, CancellationToken cancellationToken);

    /// <summary>Stages a new CRM client for insertion.</summary>
    /// <param name="client">The client to add.</param>
    void AddClient(Client client);

    /// <summary>Lists CRM clients (optionally filtered by status), newest first.</summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Client>> ListClientsAsync(ClientStatus? status, CancellationToken cancellationToken);

    /// <summary>Loads a client for update (change-tracked), or null if not present.</summary>
    /// <param name="id">The client id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Client?> GetClientAsync(ClientId id, CancellationToken cancellationToken);

    /// <summary>Counts a client's contacts.</summary>
    /// <param name="clientId">The client id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<int> CountContactsAsync(ClientId clientId, CancellationToken cancellationToken);

    /// <summary>Stages a new client contact for insertion.</summary>
    /// <param name="contact">The contact to add.</param>
    void AddClientContact(ClientContact contact);

    /// <summary>Removes a client contact.</summary>
    /// <param name="contact">The contact to remove.</param>
    void RemoveClientContact(ClientContact contact);

    /// <summary>Loads a client contact for update, or null if not present.</summary>
    /// <param name="id">The contact id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ClientContact?> GetClientContactAsync(ClientContactId id, CancellationToken cancellationToken);

    /// <summary>Lists a client's contacts, primary first then by name.</summary>
    /// <param name="clientId">The client id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ClientContact>> ListContactsForClientAsync(ClientId clientId, CancellationToken cancellationToken);

    /// <summary>Stages a new offer for insertion.</summary>
    /// <param name="offer">The offer to add.</param>
    void AddOffer(Offer offer);

    /// <summary>Loads an offer for update (change-tracked), or null if not present.</summary>
    /// <param name="id">The offer id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Offer?> GetOfferAsync(OfferId id, CancellationToken cancellationToken);

    /// <summary>Lists an application's offers, newest first.</summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Offer>> ListOffersForApplicationAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>Stages a new onboarding checklist for insertion.</summary>
    /// <param name="checklist">The checklist to add.</param>
    void AddOnboardingChecklist(OnboardingChecklist checklist);

    /// <summary>Loads the onboarding checklist for an application, or null if none.</summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<OnboardingChecklist?> GetChecklistByApplicationAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>Loads an onboarding checklist by id, or null if not present.</summary>
    /// <param name="id">The checklist id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<OnboardingChecklist?> GetChecklistAsync(OnboardingChecklistId id, CancellationToken cancellationToken);

    /// <summary>Stages a new onboarding task for insertion.</summary>
    /// <param name="task">The task to add.</param>
    void AddOnboardingTask(OnboardingTask task);

    /// <summary>Removes an onboarding task.</summary>
    /// <param name="task">The task to remove.</param>
    void RemoveOnboardingTask(OnboardingTask task);

    /// <summary>Loads an onboarding task for update, or null if not present.</summary>
    /// <param name="id">The task id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<OnboardingTask?> GetOnboardingTaskAsync(OnboardingTaskId id, CancellationToken cancellationToken);

    /// <summary>Lists a checklist's tasks in order.</summary>
    /// <param name="checklistId">The checklist id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<OnboardingTask>> ListTasksForChecklistAsync(OnboardingChecklistId checklistId, CancellationToken cancellationToken);

    /// <summary>Loads a requisition's enrichment detail (change-tracked), or null if none.</summary>
    /// <param name="requestId">The requisition id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RequisitionDetail?> GetRequisitionDetailAsync(Guid requestId, CancellationToken cancellationToken);

    /// <summary>Stages a new requisition detail for insertion.</summary>
    /// <param name="detail">The detail to add.</param>
    void AddRequisitionDetail(RequisitionDetail detail);

    /// <summary>Lists a requisition's tags, alphabetically.</summary>
    /// <param name="requestId">The requisition id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<RequisitionTag>> ListRequisitionTagsAsync(Guid requestId, CancellationToken cancellationToken);

    /// <summary>Whether the requisition already has the given (normalised) tag.</summary>
    /// <param name="requestId">The requisition id.</param>
    /// <param name="label">The normalised label.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> RequisitionTagExistsAsync(Guid requestId, string label, CancellationToken cancellationToken);

    /// <summary>Loads a requisition tag by normalised label, or null if not present.</summary>
    /// <param name="requestId">The requisition id.</param>
    /// <param name="label">The normalised label.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RequisitionTag?> GetRequisitionTagAsync(Guid requestId, string label, CancellationToken cancellationToken);

    /// <summary>Stages a new requisition tag for insertion.</summary>
    /// <param name="tag">The tag to add.</param>
    void AddRequisitionTag(RequisitionTag tag);

    /// <summary>Removes a requisition tag.</summary>
    /// <param name="tag">The tag to remove.</param>
    void RemoveRequisitionTag(RequisitionTag tag);

    /// <summary>Loads a requisition's approval record (change-tracked), or null if none.</summary>
    /// <param name="requestId">The requisition id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RequisitionApproval?> GetApprovalAsync(Guid requestId, CancellationToken cancellationToken);

    /// <summary>Stages a new approval record for insertion.</summary>
    /// <param name="approval">The approval to add.</param>
    void AddApproval(RequisitionApproval approval);

    /// <summary>Stages a new email campaign for insertion.</summary>
    /// <param name="campaign">The campaign to add.</param>
    void AddCampaign(EmailCampaign campaign);

    /// <summary>Loads a campaign by id (change-tracked), or null if not present.</summary>
    /// <param name="id">The campaign id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<EmailCampaign?> GetCampaignAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lists all campaigns, newest first.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<EmailCampaign>> ListCampaignsAsync(CancellationToken cancellationToken);

    /// <summary>Stages a new campaign recipient for insertion.</summary>
    /// <param name="recipient">The recipient to add.</param>
    void AddCampaignRecipient(CampaignRecipient recipient);

    /// <summary>Removes a campaign recipient.</summary>
    /// <param name="recipient">The recipient to remove.</param>
    void RemoveCampaignRecipient(CampaignRecipient recipient);

    /// <summary>Whether the campaign already has the given (normalised) recipient email.</summary>
    /// <param name="campaignId">The campaign id.</param>
    /// <param name="email">The normalised email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> CampaignRecipientExistsAsync(Guid campaignId, string email, CancellationToken cancellationToken);

    /// <summary>Loads a campaign recipient by normalised email, or null if not present.</summary>
    /// <param name="campaignId">The campaign id.</param>
    /// <param name="email">The normalised email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<CampaignRecipient?> GetCampaignRecipientAsync(Guid campaignId, string email, CancellationToken cancellationToken);

    /// <summary>Lists a campaign's recipients.</summary>
    /// <param name="campaignId">The campaign id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<CampaignRecipient>> ListCampaignRecipientsAsync(Guid campaignId, CancellationToken cancellationToken);

    /// <summary>Stages a new application message for insertion.</summary>
    /// <param name="message">The message to add.</param>
    void AddApplicationMessage(ApplicationMessage message);

    /// <summary>Lists an application's messages, oldest first (read-only).</summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ApplicationMessage>> ListApplicationMessagesAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>Lists an application's messages for update (change-tracked).</summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ApplicationMessage>> ListApplicationMessagesTrackedAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>Stages a new interview attendee for insertion.</summary>
    /// <param name="attendee">The attendee to add.</param>
    void AddInterviewAttendee(InterviewAttendee attendee);

    /// <summary>Removes an interview attendee.</summary>
    /// <param name="attendee">The attendee to remove.</param>
    void RemoveInterviewAttendee(InterviewAttendee attendee);

    /// <summary>Loads an interview attendee by id, or null if not present.</summary>
    /// <param name="id">The attendee id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<InterviewAttendee?> GetInterviewAttendeeAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lists an interview's panel attendees, in add order.</summary>
    /// <param name="interviewId">The interview id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<InterviewAttendee>> ListInterviewAttendeesAsync(Guid interviewId, CancellationToken cancellationToken);

    /// <summary>Stages a new application-form question for insertion.</summary>
    /// <param name="question">The question to add.</param>
    void AddFormQuestion(ApplicationFormQuestion question);

    /// <summary>Removes an application-form question.</summary>
    /// <param name="question">The question to remove.</param>
    void RemoveFormQuestion(ApplicationFormQuestion question);

    /// <summary>Loads a form question by id, or null if not present.</summary>
    /// <param name="id">The question id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ApplicationFormQuestion?> GetFormQuestionAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lists a requisition's application-form questions, ascending sort order.</summary>
    /// <param name="requestId">The requisition id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ApplicationFormQuestion>> ListFormQuestionsAsync(Guid requestId, CancellationToken cancellationToken);

    /// <summary>Stages a new application answer for insertion.</summary>
    /// <param name="answer">The answer to add.</param>
    void AddApplicationAnswer(ApplicationAnswer answer);

    /// <summary>Removes an application answer.</summary>
    /// <param name="answer">The answer to remove.</param>
    void RemoveApplicationAnswer(ApplicationAnswer answer);

    /// <summary>Lists an application's answers (no-tracking, for read).</summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ApplicationAnswer>> ListApplicationAnswersAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>Lists an application's answers (tracked, for replace-on-resubmit).</summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ApplicationAnswer>> ListApplicationAnswersTrackedAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>Stages a new job template for insertion.</summary>
    /// <param name="template">The template to add.</param>
    void AddJobTemplate(JobTemplate template);

    /// <summary>Removes a job template.</summary>
    /// <param name="template">The template to remove.</param>
    void RemoveJobTemplate(JobTemplate template);

    /// <summary>Loads a job template by id, or null if not present.</summary>
    /// <param name="id">The template id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<JobTemplate?> GetJobTemplateAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lists all job templates, newest first.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<JobTemplate>> ListJobTemplatesAsync(CancellationToken cancellationToken);

    /// <summary>Whether a template with the given name already exists (case-insensitive).</summary>
    /// <param name="name">The template name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> JobTemplateNameExistsAsync(string name, CancellationToken cancellationToken);

    /// <summary>Commits staged changes to the data store.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Returns aggregate recruitment statistics (funnel, hires trend, matching, top cities).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregate recruitment statistics.</returns>
    Task<RecruitmentStatsDto> GetStatsAsync(CancellationToken cancellationToken);

    /// <summary>Returns time-to-hire and source-of-hire metrics.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregate hiring metrics.</returns>
    Task<HiringMetricsDto> GetHiringMetricsAsync(CancellationToken cancellationToken);
}
