namespace Illumin360.Admin.IntegrationEvents;

/// <summary>
/// Integration event published when an administrator approves or rejects a verification. Consumed by
/// downstream services (e.g. notifications to inform the entity). The broker exchange is the
/// namespace-qualified type name <c>Illumin360.Admin.IntegrationEvents:VerificationDecided</c>.
/// </summary>
/// <param name="VerificationId">The verification identity.</param>
/// <param name="Entity">Entity under review.</param>
/// <param name="Outcome">"Approved" or "Rejected".</param>
/// <param name="DecidedBy">Deciding admin username.</param>
/// <param name="OccurredOn">When the decision occurred (UTC).</param>
public sealed record VerificationDecided(
    Guid VerificationId, string Entity, string Outcome, string DecidedBy, DateTimeOffset OccurredOn);
