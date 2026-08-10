namespace Illumin360.Email;

/// <summary>SMTP configuration (bound from the <c>Email</c> section; defaults target the dev Mailpit).</summary>
public sealed class EmailOptions
{
    /// <summary>SMTP host (Mailpit service name in-cluster).</summary>
    public string Host { get; set; } = "mailpit";

    /// <summary>SMTP port.</summary>
    public int Port { get; set; } = 1025;

    /// <summary>From address.</summary>
    public string FromAddress { get; set; } = "no-reply@illumin360.test";

    /// <summary>From display name.</summary>
    public string FromName { get; set; } = "Illumin360";
}
