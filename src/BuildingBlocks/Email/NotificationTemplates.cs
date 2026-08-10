namespace Illumin360.Email;

/// <summary>A rendered email (subject + HTML body).</summary>
/// <param name="Subject">Subject line.</param>
/// <param name="HtmlBody">HTML body.</param>
public sealed record EmailContent(string Subject, string HtmlBody);

/// <summary>Deterministic templates for Illumin360 notification emails.</summary>
public static class NotificationTemplates
{
    /// <summary>Welcome email for a newly-registered talent.</summary>
    /// <param name="name">Recipient's display name (falls back to a generic greeting).</param>
    /// <returns>The rendered email.</returns>
    public static EmailContent Welcome(string? name)
    {
        var greeting = string.IsNullOrWhiteSpace(name) ? "there" : name.Trim();
        return new EmailContent(
            "Welcome to Illumin360",
            $"<p>Hi {greeting},</p><p>Welcome to Illumin360 — your profile is live. Complete it and upload your CV to get matched to roles.</p><p>— The Illumin360 team</p>");
    }

    /// <summary>Acknowledges a freshly-submitted application.</summary>
    /// <returns>The rendered email.</returns>
    public static EmailContent ApplicationReceived() => new(
        "We received your application",
        "<p>Thanks — we've received your application. We'll email you as it progresses through review.</p><p>— The Illumin360 team</p>");

    /// <summary>Notifies a talent that their application's status changed.</summary>
    /// <param name="roleTitle">The role applied to.</param>
    /// <param name="status">The new pipeline status.</param>
    /// <returns>The rendered email.</returns>
    public static EmailContent ApplicationStatusChanged(string roleTitle, string status)
    {
        var role = string.IsNullOrWhiteSpace(roleTitle) ? "a role" : roleTitle.Trim();
        return new EmailContent(
            $"Update on your application for {role}",
            $"<p>Your application for <strong>{role}</strong> is now <strong>{status}</strong>.</p><p>Sign in to Illumin360 to see the details.</p>");
    }
}
