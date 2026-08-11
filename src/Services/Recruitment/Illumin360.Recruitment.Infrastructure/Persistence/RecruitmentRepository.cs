using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Microsoft.EntityFrameworkCore;
using ApplicationId = Illumin360.Recruitment.Domain.ApplicationId;

namespace Illumin360.Recruitment.Infrastructure.Persistence;

/// <summary>EF Core adapter implementing <see cref="IRecruitmentRepository"/>.</summary>
public sealed class RecruitmentRepository(RecruitmentDbContext db) : IRecruitmentRepository
{
    // Funnel stage ordering for presentation (applied → reviewed → shortlisted → hired → rejected).
    private static readonly string[] StageOrder = ["applied", "reviewed", "shortlisted", "hired", "rejected"];

    private readonly RecruitmentDbContext _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecruitmentRequest>> ListAsync(
        string? city, string? status, int skip, int take, CancellationToken cancellationToken)
    {
        var query = _db.Requests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(city))
        {
            // Case-insensitive match translated to PostgreSQL ILIKE (avoids client-side ToLower).
            query = query.Where(r => EF.Functions.ILike(r.City, city));
        }

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<RequestStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(r => r.Status == parsedStatus);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RecruitmentRequest?> GetByIdAsync(RequestId id, CancellationToken cancellationToken)
        => await _db.Requests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecruitmentApplication>> ListApplicationsAsync(
        RequestId requestId, int skip, int take, CancellationToken cancellationToken)
        => await _db.Applications.AsNoTracking()
            .Where(a => a.RequestId == requestId)
            .OrderByDescending(a => a.MatchScore)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void Add(RecruitmentRequest request) => _db.Requests.Add(request);

    /// <inheritdoc />
    public async Task<bool> HasApplicationAsync(RequestId requestId, Guid talentId, CancellationToken cancellationToken)
        => await _db.Applications.AsNoTracking()
            .AnyAsync(a => a.RequestId == requestId && a.TalentId == talentId, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void AddApplication(RecruitmentApplication application) => _db.Applications.Add(application);

    /// <inheritdoc />
    public async Task<RecruitmentApplication?> GetApplicationAsync(ApplicationId id, CancellationToken cancellationToken)
        => await _db.Applications.FirstOrDefaultAsync(a => a.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddSavedSearch(SavedSearch savedSearch) => _db.SavedSearches.Add(savedSearch);

    /// <inheritdoc />
    public void RemoveSavedSearch(SavedSearch savedSearch) => _db.SavedSearches.Remove(savedSearch);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SavedSearch>> ListSavedSearchesForTalentAsync(Guid talentId, CancellationToken cancellationToken)
        => await _db.SavedSearches.AsNoTracking()
            .Where(s => s.TalentId == talentId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<SavedSearch?> GetSavedSearchAsync(SavedSearchId id, CancellationToken cancellationToken)
        => await _db.SavedSearches.FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SavedSearch>> ListAlertEnabledSavedSearchesAsync(CancellationToken cancellationToken)
        => await _db.SavedSearches.AsNoTracking()
            .Where(s => s.AlertsEnabled)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void AddInterview(Interview interview) => _db.Interviews.Add(interview);

    /// <inheritdoc />
    public async Task<Interview?> GetInterviewAsync(InterviewId id, CancellationToken cancellationToken)
        => await _db.Interviews.FirstOrDefaultAsync(i => i.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Interview>> ListInterviewsForApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
        => await _db.Interviews.AsNoTracking()
            .Where(i => i.ApplicationId == applicationId)
            .OrderBy(i => i.ScheduledAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecruitmentApplication>> ListApplicationsForTalentAsync(Guid talentId, int skip, int take, CancellationToken cancellationToken)
        => await _db.Applications.AsNoTracking()
            .Where(a => a.TalentId == talentId)
            .OrderByDescending(a => a.AppliedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void AddClient(Client client) => _db.Clients.Add(client);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Client>> ListClientsAsync(ClientStatus? status, CancellationToken cancellationToken)
    {
        var query = _db.Clients.AsNoTracking();
        if (status is { } s)
        {
            query = query.Where(c => c.Status == s);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Client?> GetClientAsync(ClientId id, CancellationToken cancellationToken)
        => await _db.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<int> CountContactsAsync(ClientId clientId, CancellationToken cancellationToken)
        => await _db.ClientContacts.AsNoTracking()
            .CountAsync(c => c.ClientId == clientId, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void AddClientContact(ClientContact contact) => _db.ClientContacts.Add(contact);

    /// <inheritdoc />
    public void RemoveClientContact(ClientContact contact) => _db.ClientContacts.Remove(contact);

    /// <inheritdoc />
    public async Task<ClientContact?> GetClientContactAsync(ClientContactId id, CancellationToken cancellationToken)
        => await _db.ClientContacts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClientContact>> ListContactsForClientAsync(ClientId clientId, CancellationToken cancellationToken)
        => await _db.ClientContacts.AsNoTracking()
            .Where(c => c.ClientId == clientId)
            .OrderByDescending(c => c.IsPrimary)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void AddOffer(Offer offer) => _db.Offers.Add(offer);

    /// <inheritdoc />
    public async Task<Offer?> GetOfferAsync(OfferId id, CancellationToken cancellationToken)
        => await _db.Offers.FirstOrDefaultAsync(o => o.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Offer>> ListOffersForApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
        => await _db.Offers.AsNoTracking()
            .Where(o => o.ApplicationId == applicationId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void AddOnboardingChecklist(OnboardingChecklist checklist) => _db.OnboardingChecklists.Add(checklist);

    /// <inheritdoc />
    public async Task<OnboardingChecklist?> GetChecklistByApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
        => await _db.OnboardingChecklists.FirstOrDefaultAsync(c => c.ApplicationId == applicationId, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<OnboardingChecklist?> GetChecklistAsync(OnboardingChecklistId id, CancellationToken cancellationToken)
        => await _db.OnboardingChecklists.FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddOnboardingTask(OnboardingTask task) => _db.OnboardingTasks.Add(task);

    /// <inheritdoc />
    public void RemoveOnboardingTask(OnboardingTask task) => _db.OnboardingTasks.Remove(task);

    /// <inheritdoc />
    public async Task<OnboardingTask?> GetOnboardingTaskAsync(OnboardingTaskId id, CancellationToken cancellationToken)
        => await _db.OnboardingTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<OnboardingTask>> ListTasksForChecklistAsync(OnboardingChecklistId checklistId, CancellationToken cancellationToken)
        => await _db.OnboardingTasks.AsNoTracking()
            .Where(t => t.ChecklistId == checklistId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<RequisitionDetail?> GetRequisitionDetailAsync(Guid requestId, CancellationToken cancellationToken)
        => await _db.RequisitionDetails.FirstOrDefaultAsync(d => d.RequestId == requestId, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddRequisitionDetail(RequisitionDetail detail) => _db.RequisitionDetails.Add(detail);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RequisitionTag>> ListRequisitionTagsAsync(Guid requestId, CancellationToken cancellationToken)
        => await _db.RequisitionTags.AsNoTracking()
            .Where(t => t.RequestId == requestId)
            .OrderBy(t => t.Label)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> RequisitionTagExistsAsync(Guid requestId, string label, CancellationToken cancellationToken)
        => await _db.RequisitionTags.AsNoTracking()
            .AnyAsync(t => t.RequestId == requestId && t.Label == label, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<RequisitionTag?> GetRequisitionTagAsync(Guid requestId, string label, CancellationToken cancellationToken)
        => await _db.RequisitionTags.FirstOrDefaultAsync(t => t.RequestId == requestId && t.Label == label, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddRequisitionTag(RequisitionTag tag) => _db.RequisitionTags.Add(tag);

    /// <inheritdoc />
    public void RemoveRequisitionTag(RequisitionTag tag) => _db.RequisitionTags.Remove(tag);

    /// <inheritdoc />
    public async Task<RequisitionApproval?> GetApprovalAsync(Guid requestId, CancellationToken cancellationToken)
        => await _db.RequisitionApprovals.FirstOrDefaultAsync(a => a.RequestId == requestId, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddApproval(RequisitionApproval approval) => _db.RequisitionApprovals.Add(approval);

    /// <inheritdoc />
    public void AddCampaign(EmailCampaign campaign) => _db.EmailCampaigns.Add(campaign);

    /// <inheritdoc />
    public async Task<EmailCampaign?> GetCampaignAsync(Guid id, CancellationToken cancellationToken)
        => await _db.EmailCampaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmailCampaign>> ListCampaignsAsync(CancellationToken cancellationToken)
        => await _db.EmailCampaigns.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddCampaignRecipient(CampaignRecipient recipient) => _db.CampaignRecipients.Add(recipient);

    /// <inheritdoc />
    public void RemoveCampaignRecipient(CampaignRecipient recipient) => _db.CampaignRecipients.Remove(recipient);

    /// <inheritdoc />
    public async Task<bool> CampaignRecipientExistsAsync(Guid campaignId, string email, CancellationToken cancellationToken)
        => await _db.CampaignRecipients.AsNoTracking()
            .AnyAsync(r => r.CampaignId == campaignId && r.Email == email, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<CampaignRecipient?> GetCampaignRecipientAsync(Guid campaignId, string email, CancellationToken cancellationToken)
        => await _db.CampaignRecipients.FirstOrDefaultAsync(r => r.CampaignId == campaignId && r.Email == email, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CampaignRecipient>> ListCampaignRecipientsAsync(Guid campaignId, CancellationToken cancellationToken)
        => await _db.CampaignRecipients.AsNoTracking()
            .Where(r => r.CampaignId == campaignId)
            .OrderBy(r => r.Email)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddApplicationMessage(ApplicationMessage message) => _db.ApplicationMessages.Add(message);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApplicationMessage>> ListApplicationMessagesAsync(Guid applicationId, CancellationToken cancellationToken)
        => await _db.ApplicationMessages.AsNoTracking()
            .Where(m => m.ApplicationId == applicationId)
            .OrderBy(m => m.SentAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApplicationMessage>> ListApplicationMessagesTrackedAsync(Guid applicationId, CancellationToken cancellationToken)
        => await _db.ApplicationMessages
            .Where(m => m.ApplicationId == applicationId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddInterviewAttendee(InterviewAttendee attendee) => _db.InterviewAttendees.Add(attendee);

    /// <inheritdoc />
    public void RemoveInterviewAttendee(InterviewAttendee attendee) => _db.InterviewAttendees.Remove(attendee);

    /// <inheritdoc />
    public async Task<InterviewAttendee?> GetInterviewAttendeeAsync(Guid id, CancellationToken cancellationToken)
        => await _db.InterviewAttendees.FirstOrDefaultAsync(a => a.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<InterviewAttendee>> ListInterviewAttendeesAsync(Guid interviewId, CancellationToken cancellationToken)
        => await _db.InterviewAttendees.AsNoTracking()
            .Where(a => a.InterviewId == interviewId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void AddJobTemplate(JobTemplate template) => _db.JobTemplates.Add(template);

    /// <inheritdoc />
    public void RemoveJobTemplate(JobTemplate template) => _db.JobTemplates.Remove(template);

    /// <inheritdoc />
    public async Task<JobTemplate?> GetJobTemplateAsync(Guid id, CancellationToken cancellationToken)
        => await _db.JobTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobTemplate>> ListJobTemplatesAsync(CancellationToken cancellationToken)
        => await _db.JobTemplates.AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> JobTemplateNameExistsAsync(string name, CancellationToken cancellationToken)
        => await _db.JobTemplates.AsNoTracking()
            .AnyAsync(t => EF.Functions.ILike(t.Name, name), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<RecruitmentStatsDto> GetStatsAsync(CancellationToken cancellationToken)
    {
        var requests = _db.Requests.AsNoTracking();
        var apps = _db.Applications.AsNoTracking();

        var totalRequests = await requests.CountAsync(cancellationToken).ConfigureAwait(false);
        var filledRequests = await requests
            .CountAsync(r => r.Status == RequestStatus.Filled, cancellationToken).ConfigureAwait(false);
        var openRequests = await requests
            .CountAsync(r => r.Status == RequestStatus.Open, cancellationToken).ConfigureAwait(false);

        var totalApplications = await apps.CountAsync(cancellationToken).ConfigureAwait(false);
        var totalHires = await apps.CountAsync(a => a.IsHire, cancellationToken).ConfigureAwait(false);
        var avgScore = totalApplications == 0
            ? 0.0
            : (double)await apps.AverageAsync(a => a.MatchScore, cancellationToken).ConfigureAwait(false);

        var funnelRaw = await apps
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byTypeRaw = await apps
            .GroupBy(a => a.TalentType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var cityRaw = await requests
            .GroupBy(r => r.City)
            .Select(g => new { City = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(12)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var trendRaw = await apps
            .GroupBy(a => new { a.AppliedAt.Year, a.AppliedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Applications = g.Count(),
                Hires = g.Sum(a => a.IsHire ? 1 : 0),
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var funnel = funnelRaw
            .OrderBy(x => Array.IndexOf(StageOrder, x.Status))
            .Select(x => new CountByLabel(x.Status, x.Count))
            .ToList();

        var byTalentType = byTypeRaw
            .Select(x => new CountByLabel(x.Type, x.Count))
            .OrderByDescending(x => x.Count)
            .ToList();

        var topCities = cityRaw.Select(x => new CountByLabel(x.City, x.Count)).ToList();

        var trend = trendRaw
            .Select(x => new MonthlyPoint($"{x.Year:D4}-{x.Month:D2}", x.Applications, x.Hires))
            .ToList();

        return new RecruitmentStatsDto(
            totalRequests,
            openRequests,
            filledRequests,
            totalApplications,
            totalHires,
            Math.Round(avgScore, 1),
            funnel,
            byTalentType,
            topCities,
            trend);
    }
}
