namespace Illumin360.Employers.IntegrationEvents;

/// <summary>
/// Published when an employer company profile is registered. Consumed by other services
/// (e.g. the Admin account directory). The broker exchange is the namespace-qualified type name,
/// so publisher and consumer must share this exact type.
/// </summary>
/// <param name="EmployerId">The new employer's id.</param>
/// <param name="CompanyName">The company name.</param>
/// <param name="City">The company's city (used as the directory region).</param>
/// <param name="OccurredOn">When the registration occurred (UTC).</param>
public sealed record EmployerRegistered(Guid EmployerId, string CompanyName, string City, DateTimeOffset OccurredOn);
