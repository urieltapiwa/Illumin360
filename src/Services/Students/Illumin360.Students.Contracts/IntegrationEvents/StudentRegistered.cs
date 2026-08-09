namespace Illumin360.Students.IntegrationEvents;

/// <summary>
/// Integration event published when a new student is registered. Consumed by downstream services
/// (e.g. notifications). The broker exchange is the namespace-qualified type name
/// <c>Illumin360.Students.IntegrationEvents:StudentRegistered</c>, so publisher and consumers must
/// reference this exact type. Distinct from the in-process domain event of the same simple name.
/// </summary>
/// <param name="StudentId">The new student's identity.</param>
/// <param name="FullName">The student's full name.</param>
/// <param name="OccurredOn">When registration occurred (UTC).</param>
public sealed record StudentRegistered(Guid StudentId, string FullName, DateTimeOffset OccurredOn);
