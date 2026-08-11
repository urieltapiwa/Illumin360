using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>Lifecycle of a bulk email campaign.</summary>
public enum CampaignStatus
{
    /// <summary>Being composed; recipients can be edited.</summary>
    Draft,

    /// <summary>Sent to its recipients (terminal).</summary>
    Sent,
}

/// <summary>
/// A bulk email campaign a recruiter composes and sends to a set of recipients. Owned + migration-managed
/// by the service. Recipients are modelled as <see cref="CampaignRecipient"/> rows.
/// </summary>
public sealed class EmailCampaign : Entity<Guid>
{
    private EmailCampaign(Guid id)
        : base(id)
    {
    }

    /// <summary>Internal campaign name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Email subject.</summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>Email body.</summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>Campaign status.</summary>
    public CampaignStatus Status { get; private set; }

    /// <summary>Number of recipients emailed (set on send).</summary>
    public int RecipientCount { get; private set; }

    /// <summary>When the campaign was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When the campaign was sent (UTC), if applicable.</summary>
    public DateTimeOffset? SentAt { get; private set; }

    /// <summary>Creates a draft campaign.</summary>
    /// <param name="name">Internal name (required).</param>
    /// <param name="subject">Subject (required).</param>
    /// <param name="body">Body (required, ≤ 10000 chars).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The campaign, or a validation error.</returns>
    public static Result<EmailCampaign> Create(string name, string subject, string body, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("campaign.name_required", "A campaign name is required.");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Error.Validation("campaign.subject_required", "A subject is required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return Error.Validation("campaign.body_required", "A body is required.");
        }

        if (body.Length > 10000)
        {
            return Error.Validation("campaign.body_too_long", "A body must be 10000 characters or fewer.");
        }

        return new EmailCampaign(Guid.NewGuid())
        {
            Name = name.Trim(),
            Subject = subject.Trim(),
            Body = body.Trim(),
            Status = CampaignStatus.Draft,
            CreatedAt = createdAt,
        };
    }

    /// <summary>Marks the campaign sent to the given number of recipients.</summary>
    /// <param name="recipientCount">Number of recipients emailed (must be &gt; 0).</param>
    /// <param name="at">Send timestamp (UTC).</param>
    /// <returns>Success, or a validation/conflict error.</returns>
    public Result<EmailCampaign> MarkSent(int recipientCount, DateTimeOffset at)
    {
        if (Status == CampaignStatus.Sent)
        {
            return Error.Conflict("campaign.already_sent", "This campaign has already been sent.");
        }

        if (recipientCount < 1)
        {
            return Error.Validation("campaign.no_recipients", "A campaign needs at least one recipient.");
        }

        Status = CampaignStatus.Sent;
        RecipientCount = recipientCount;
        SentAt = at;
        return this;
    }
}

/// <summary>A recipient of an <see cref="EmailCampaign"/> (unique per campaign, normalised).</summary>
public sealed class CampaignRecipient : Entity<Guid>
{
    private CampaignRecipient(Guid id)
        : base(id)
    {
    }

    /// <summary>The owning campaign.</summary>
    public Guid CampaignId { get; private init; }

    /// <summary>Recipient email (lower-cased).</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Creates a recipient, validating the email.</summary>
    /// <param name="campaignId">The owning campaign (required).</param>
    /// <param name="email">The recipient email (required, must look like an address).</param>
    /// <returns>The recipient, or a validation error.</returns>
    public static Result<CampaignRecipient> Create(Guid campaignId, string email)
    {
        if (campaignId == Guid.Empty)
        {
            return Error.Validation("campaign.campaign_required", "A campaign id is required.");
        }

        if (string.IsNullOrWhiteSpace(email) || !LooksLikeEmail(email))
        {
            return Error.Validation("recipient.email_invalid", "A valid email address is required.");
        }

        return new CampaignRecipient(Guid.NewGuid()) { CampaignId = campaignId, Email = email.Trim().ToLowerInvariant() };
    }

    private static bool LooksLikeEmail(string value)
    {
        var trimmed = value.Trim();
        var at = trimmed.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at < trimmed.Length - 1;
    }
}
