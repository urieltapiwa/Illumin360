using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// The channel a candidate arrived through for an application ("how they got here") — e.g. referral,
/// campaign, careers site, job board, agency, walk-in. Service-owned + migration-managed, keyed 1:1 by
/// the (externally-seeded) application id. Distinct from talent type (professional/student).
/// </summary>
public sealed class ApplicationSource : Entity<Guid>
{
    /// <summary>The recognised default when no channel is supplied.</summary>
    public const string DefaultChannel = "direct";

    private ApplicationSource(Guid id)
        : base(id)
    {
    }

    /// <summary>The application this source describes.</summary>
    public Guid ApplicationId { get; private init; }

    /// <summary>The arrival channel (normalised, lower-cased).</summary>
    public string Channel { get; private set; } = DefaultChannel;

    /// <summary>When the source was recorded (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Normalises a channel name: trims, lower-cases, and falls back to <see cref="DefaultChannel"/>.</summary>
    /// <param name="channel">The raw channel.</param>
    /// <returns>The normalised channel.</returns>
    public static string Normalize(string? channel)
        => string.IsNullOrWhiteSpace(channel) ? DefaultChannel : channel.Trim().ToLowerInvariant();

    /// <summary>Records the arrival channel for an application.</summary>
    /// <param name="applicationId">The application (required).</param>
    /// <param name="channel">The channel (normalised; defaults to <see cref="DefaultChannel"/>).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The source, or a validation error.</returns>
    public static Result<ApplicationSource> Create(Guid applicationId, string? channel, DateTimeOffset createdAt)
    {
        if (applicationId == Guid.Empty)
        {
            return Error.Validation("source.application_required", "An application id is required.");
        }

        var normalised = Normalize(channel);
        if (normalised.Length > 40)
        {
            return Error.Validation("source.channel_too_long", "A channel name must be 40 characters or fewer.");
        }

        return new ApplicationSource(Guid.NewGuid())
        {
            ApplicationId = applicationId,
            Channel = normalised,
            CreatedAt = createdAt,
        };
    }

    /// <summary>Updates the channel in place.</summary>
    /// <param name="channel">The new channel (normalised).</param>
    public void SetChannel(string? channel) => Channel = Normalize(channel);
}
