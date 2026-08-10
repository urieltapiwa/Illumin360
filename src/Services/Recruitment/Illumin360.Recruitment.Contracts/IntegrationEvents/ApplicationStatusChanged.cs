namespace Illumin360.Recruitment.IntegrationEvents;

/// <summary>
/// Integration event published (via the transactional outbox) when a recruiter advances or rejects an
/// application. Cross-service contract consumed by the Notifications worker to email the applicant.
/// </summary>
/// <param name="ApplicationId">The application's id.</param>
/// <param name="TalentId">The applicant's talent id.</param>
/// <param name="TalentType">Talent type (<c>student</c>/<c>professional</c>).</param>
/// <param name="Status">The new pipeline status.</param>
/// <param name="OccurredOn">When the decision was made (UTC).</param>
public sealed record ApplicationStatusChanged(Guid ApplicationId, Guid TalentId, string TalentType, string Status, DateTimeOffset OccurredOn);
