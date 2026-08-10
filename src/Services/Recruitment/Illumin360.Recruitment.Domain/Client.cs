using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// A client company the agency recruits for (recruiter CRM). Owned and migration-managed by the
/// Recruitment service. Contacts are modelled as a separate <see cref="ClientContact"/> entity keyed by
/// <see cref="ClientId"/>.
/// </summary>
public sealed class Client : Entity<ClientId>
{
    // EF Core materialisation constructor.
    private Client(ClientId id)
        : base(id)
    {
    }

    /// <summary>Company name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Industry / sector, if recorded.</summary>
    public string? Industry { get; private set; }

    /// <summary>City, if recorded.</summary>
    public string? City { get; private set; }

    /// <summary>Relationship status.</summary>
    public ClientStatus Status { get; private set; }

    /// <summary>Free-text notes, if any.</summary>
    public string? Notes { get; private set; }

    /// <summary>When the client was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a client, enforcing a non-empty name.</summary>
    /// <param name="name">Company name (required).</param>
    /// <param name="industry">Optional industry.</param>
    /// <param name="city">Optional city.</param>
    /// <param name="notes">Optional notes (≤ 2000 chars).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The client, or a validation error.</returns>
    public static Result<Client> Create(string name, string? industry, string? city, string? notes, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("client.name_required", "A client name is required.");
        }

        if (notes is { Length: > 2000 })
        {
            return Error.Validation("client.notes_too_long", "Notes must be 2000 characters or fewer.");
        }

        return new Client(ClientId.New())
        {
            Name = name.Trim(),
            Industry = Clean(industry),
            City = Clean(city),
            Status = ClientStatus.Prospect,
            Notes = Clean(notes),
            CreatedAt = createdAt,
        };
    }

    /// <summary>Rehydrates a fully-specified client for demo seeding / import.</summary>
    /// <param name="id">Identity.</param>
    /// <param name="name">Company name.</param>
    /// <param name="industry">Industry.</param>
    /// <param name="city">City.</param>
    /// <param name="status">Status.</param>
    /// <param name="notes">Notes.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The hydrated client.</returns>
    public static Client Seed(Guid id, string name, string? industry, string? city, ClientStatus status, string? notes, DateTimeOffset createdAt)
        => new(new ClientId(id))
        {
            Name = name,
            Industry = industry,
            City = city,
            Status = status,
            Notes = notes,
            CreatedAt = createdAt,
        };

    /// <summary>Changes the client's relationship status.</summary>
    /// <param name="status">The new status name (prospect/active/inactive).</param>
    /// <returns>Success, or a validation error.</returns>
    public Result<Client> ChangeStatus(string status)
    {
        if (!ClientStatuses.TryParse(status, out var parsed))
        {
            return Error.Validation("client.status_invalid", "Status must be one of prospect, active or inactive.");
        }

        Status = parsed;
        return this;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
