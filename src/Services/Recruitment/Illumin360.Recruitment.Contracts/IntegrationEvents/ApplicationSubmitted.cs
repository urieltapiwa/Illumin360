namespace Illumin360.Recruitment.IntegrationEvents;

/// <summary>
/// Integration event published (via the transactional outbox) when a talent applies to a recruitment
/// request. Cross-service contract consumed by the Notifications worker to send an acknowledgement email.
/// </summary>
/// <param name="ApplicationId">The new application's id.</param>
/// <param name="RequestId">The role applied to.</param>
/// <param name="TalentId">The applying talent's id.</param>
/// <param name="TalentType">Talent type (<c>student</c>/<c>professional</c>).</param>
/// <param name="OccurredOn">When the application was made (UTC).</param>
public sealed record ApplicationSubmitted(Guid ApplicationId, Guid RequestId, Guid TalentId, string TalentType, DateTimeOffset OccurredOn);
