namespace Illumin360.Recruitment.IntegrationEvents;

/// <summary>
/// Integration event published by the scheduled job-alert runner when an alert-enabled saved search has
/// matching open roles. Consumed by the Notifications worker to email the talent a digest.
/// </summary>
/// <param name="TalentId">The owning talent's id.</param>
/// <param name="SearchLabel">The saved search's label.</param>
/// <param name="MatchCount">How many open roles currently match.</param>
/// <param name="SampleTitles">A few matching role titles for the email body.</param>
/// <param name="OccurredOn">When the digest was produced (UTC).</param>
public sealed record JobAlertDigest(Guid TalentId, string SearchLabel, int MatchCount, IReadOnlyList<string> SampleTitles, DateTimeOffset OccurredOn);
