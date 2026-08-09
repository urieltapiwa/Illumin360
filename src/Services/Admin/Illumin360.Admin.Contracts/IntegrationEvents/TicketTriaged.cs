namespace Illumin360.Admin.IntegrationEvents;

/// <summary>Integration event published when a support ticket is assigned or resolved.</summary>
/// <param name="TicketId">The ticket identity.</param>
/// <param name="Status">New status.</param>
/// <param name="Assignee">Current assignee.</param>
/// <param name="OccurredOn">When it occurred (UTC).</param>
public sealed record TicketTriaged(Guid TicketId, string Status, string Assignee, DateTimeOffset OccurredOn);
