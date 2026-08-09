namespace Illumin360.Professionals.IntegrationEvents;

/// <summary>
/// Integration event published when a new professional is registered. Consumed by downstream services
/// (e.g. notifications). The broker exchange is the namespace-qualified type name
/// <c>Illumin360.Professionals.IntegrationEvents:ProfessionalRegistered</c>, so publisher and consumers must
/// reference this exact type. Distinct from the in-process domain event of the same simple name.
/// </summary>
/// <param name="ProfessionalId">The new professional's identity.</param>
/// <param name="FullName">The professional's full name.</param>
/// <param name="OccurredOn">When registration occurred (UTC).</param>
public sealed record ProfessionalRegistered(Guid ProfessionalId, string FullName, DateTimeOffset OccurredOn);
