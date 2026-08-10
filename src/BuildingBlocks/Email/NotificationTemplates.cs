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

    /// <summary>A job-alert digest for a saved search with new matching roles.</summary>
    /// <param name="label">The saved search's label.</param>
    /// <param name="count">Number of matching open roles.</param>
    /// <param name="sampleTitles">A few matching role titles.</param>
    /// <returns>The rendered email.</returns>
    public static EmailContent JobAlertDigest(string label, int count, IReadOnlyList<string> sampleTitles)
    {
        var search = string.IsNullOrWhiteSpace(label) ? "your saved search" : label.Trim();
        var items = sampleTitles is { Count: > 0 }
            ? "<ul>" + string.Concat(sampleTitles.Select(title => $"<li>{title}</li>")) + "</ul>"
            : string.Empty;
        return new EmailContent(
            $"{count} new role(s) match \"{search}\"",
            $"<p>Good news — <strong>{count}</strong> open role(s) match <strong>{search}</strong>.</p>{items}<p>Sign in to Illumin360 to view and apply.</p>");
    }

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
