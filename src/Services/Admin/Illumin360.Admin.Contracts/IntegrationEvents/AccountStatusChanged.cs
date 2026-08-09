namespace Illumin360.Admin.IntegrationEvents;

/// <summary>Integration event published when a platform account is suspended or reactivated.</summary>
/// <param name="AccountId">The account identity.</param>
/// <param name="Status">New status.</param>
/// <param name="ChangedBy">Acting admin username.</param>
/// <param name="OccurredOn">When it occurred (UTC).</param>
public sealed record AccountStatusChanged(Guid AccountId, string Status, string ChangedBy, DateTimeOffset OccurredOn);
