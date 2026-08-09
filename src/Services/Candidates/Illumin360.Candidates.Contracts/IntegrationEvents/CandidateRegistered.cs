namespace Illumin360.Candidates.IntegrationEvents;

/// <summary>
/// Integration event published to the broker (via the transactional outbox) when a new candidate
/// has been registered. This is the cross-service contract — distinct from the in-process candidate
/// domain event (charter Part 5). The message type's namespace + name determine the broker exchange
/// (<c>Illumin360.Candidates.IntegrationEvents:CandidateRegistered</c>), so publishers and consumers
/// must share this exact type.
/// </summary>
/// <param name="CandidateId">The new candidate's id.</param>
/// <param name="OccurredOn">When registration occurred (UTC).</param>
public sealed record CandidateRegistered(Guid CandidateId, DateTimeOffset OccurredOn);
