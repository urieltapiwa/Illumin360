namespace Illumin360.Recruitment.IntegrationEvents;

/// <summary>
/// Integration event published to the broker (via the transactional outbox) when a new recruitment
/// request is posted. This is the cross-service contract — distinct from the in-process domain event
/// (charter Part 5). The message type's namespace + name determine the broker exchange
/// (<c>Illumin360.Recruitment.IntegrationEvents:RecruitmentRequestPosted</c>), so publishers and
/// consumers must share this exact type.
/// </summary>
/// <param name="RequestId">The new request's id.</param>
/// <param name="CompanyId">The hiring company's id.</param>
/// <param name="OccurredOn">When the request was posted (UTC).</param>
public sealed record RecruitmentRequestPosted(Guid RequestId, Guid CompanyId, DateTimeOffset OccurredOn);
