using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>A named person at a CRM <see cref="Client"/> (hiring manager, HR contact, etc.).</summary>
public sealed class ClientContact : Entity<ClientContactId>
{
    // EF Core materialisation constructor.
    private ClientContact(ClientContactId id)
        : base(id)
    {
    }

    /// <summary>The owning client.</summary>
    public ClientId ClientId { get; private init; }

    /// <summary>Contact's name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Job title, if recorded.</summary>
    public string? Title { get; private set; }

    /// <summary>Email, if recorded.</summary>
    public string? Email { get; private set; }

    /// <summary>Phone, if recorded.</summary>
    public string? Phone { get; private set; }

    /// <summary>Whether this is the primary contact for the client.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>When the contact was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a contact for a client, enforcing a non-empty name and a plausible email.</summary>
    /// <param name="clientId">Owning client (required).</param>
    /// <param name="name">Contact name (required).</param>
    /// <param name="title">Optional job title.</param>
    /// <param name="email">Optional email (must look like an address when present).</param>
    /// <param name="phone">Optional phone.</param>
    /// <param name="isPrimary">Whether this is the primary contact.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The contact, or a validation error.</returns>
    public static Result<ClientContact> Create(ClientId clientId, string name, string? title, string? email, string? phone, bool isPrimary, DateTimeOffset createdAt)
    {
        if (clientId.Value == Guid.Empty)
        {
            return Error.Validation("contact.client_required", "A client id is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("contact.name_required", "A contact name is required.");
        }

        if (!string.IsNullOrWhiteSpace(email) && !LooksLikeEmail(email))
        {
            return Error.Validation("contact.email_invalid", "The email address is not valid.");
        }

        return new ClientContact(ClientContactId.New())
        {
            ClientId = clientId,
            Name = name.Trim(),
            Title = Clean(title),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(),
            Phone = Clean(phone),
            IsPrimary = isPrimary,
            CreatedAt = createdAt,
        };
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool LooksLikeEmail(string value)
    {
        var trimmed = value.Trim();
        var at = trimmed.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at < trimmed.Length - 1;
    }
}
