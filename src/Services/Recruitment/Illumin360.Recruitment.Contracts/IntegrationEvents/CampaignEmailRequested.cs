namespace Illumin360.Recruitment.IntegrationEvents;

/// <summary>
/// Integration event published (one per recipient) when a recruiter sends a bulk email campaign. Consumed
/// by the Notifications worker, which delivers the email over SMTP.
/// </summary>
/// <param name="CampaignId">The campaign this email belongs to.</param>
/// <param name="To">The recipient email address.</param>
/// <param name="Subject">The email subject.</param>
/// <param name="Body">The email body (plain text / simple HTML).</param>
/// <param name="OccurredOn">When the campaign was sent (UTC).</param>
public sealed record CampaignEmailRequested(Guid CampaignId, string To, string Subject, string Body, DateTimeOffset OccurredOn);
