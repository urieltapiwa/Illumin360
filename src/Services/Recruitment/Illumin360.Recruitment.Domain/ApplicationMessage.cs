using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>Which side of an application conversation sent a message.</summary>
public enum MessageSender
{
    /// <summary>Sent by the employer / recruiter.</summary>
    Recruiter,

    /// <summary>Sent by the candidate / talent.</summary>
    Talent,
}

/// <summary>Parsing helpers for <see cref="MessageSender"/>.</summary>
public static class MessageSenders
{
    /// <summary>Parses a sender name case-insensitively (recruiter/talent).</summary>
    /// <param name="value">The sender name.</param>
    /// <param name="sender">The parsed sender when successful.</param>
    /// <returns>True if recognised.</returns>
    public static bool TryParse(string? value, out MessageSender sender)
        => Enum.TryParse(value, ignoreCase: true, out sender) && Enum.IsDefined(sender);

    /// <summary>The canonical lower-case wire name (e.g. <c>recruiter</c>).</summary>
    /// <param name="sender">The sender.</param>
    /// <returns>The lower-case name.</returns>
    public static string ToWire(this MessageSender sender) => sender.ToString().ToLowerInvariant();
}

/// <summary>
/// A message in the conversation between the employer/recruiter and a candidate, anchored to an
/// application. Owned + migration-managed by the service.
/// </summary>
public sealed class ApplicationMessage : Entity<Guid>
{
    private ApplicationMessage(Guid id)
        : base(id)
    {
    }

    /// <summary>The application the conversation is about.</summary>
    public Guid ApplicationId { get; private init; }

    /// <summary>Which side sent the message.</summary>
    public MessageSender Sender { get; private set; }

    /// <summary>Display name of the sender.</summary>
    public string SenderName { get; private set; } = string.Empty;

    /// <summary>The message body.</summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>When the message was sent (UTC).</summary>
    public DateTimeOffset SentAt { get; private set; }

    /// <summary>When the message was read by the other side (UTC), or null if unread.</summary>
    public DateTimeOffset? ReadAt { get; private set; }

    /// <summary>Whether the message has been read.</summary>
    public bool IsRead => ReadAt is not null;

    /// <summary>Posts a message to an application conversation.</summary>
    /// <param name="applicationId">The application (required).</param>
    /// <param name="sender">The sender side name (recruiter/talent).</param>
    /// <param name="senderName">Display name of the sender (required).</param>
    /// <param name="body">Message body (required, ≤ 4000 chars).</param>
    /// <param name="sentAt">Sent timestamp (UTC).</param>
    /// <returns>The message, or a validation error.</returns>
    public static Result<ApplicationMessage> Post(Guid applicationId, string? sender, string senderName, string body, DateTimeOffset sentAt)
    {
        if (applicationId == Guid.Empty)
        {
            return Error.Validation("message.application_required", "An application id is required.");
        }

        if (!MessageSenders.TryParse(sender, out var parsedSender))
        {
            return Error.Validation("message.sender_invalid", "Sender must be recruiter or talent.");
        }

        if (string.IsNullOrWhiteSpace(senderName))
        {
            return Error.Validation("message.sender_name_required", "A sender name is required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return Error.Validation("message.body_required", "A message body is required.");
        }

        if (body.Length > 4000)
        {
            return Error.Validation("message.body_too_long", "A message must be 4000 characters or fewer.");
        }

        return new ApplicationMessage(Guid.NewGuid())
        {
            ApplicationId = applicationId,
            Sender = parsedSender,
            SenderName = senderName.Trim(),
            Body = body.Trim(),
            SentAt = sentAt,
        };
    }

    /// <summary>Marks the message as read.</summary>
    /// <param name="at">Read timestamp (UTC).</param>
    public void MarkRead(DateTimeOffset at) => ReadAt ??= at;
}
