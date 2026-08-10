using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// A talent's saved role search (optional city + keyword) with an opt-in for job alerts. Unlike the
/// externally-seeded requests/applications tables, this is owned and migration-managed by the service.
/// </summary>
public sealed class SavedSearch : Entity<SavedSearchId>
{
    // EF Core materialisation constructor.
    private SavedSearch(SavedSearchId id) : base(id) { }

    /// <summary>The owning talent's id.</summary>
    public Guid TalentId { get; private set; }

    /// <summary>Human-readable label for the search.</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>Optional city filter.</summary>
    public string? City { get; private set; }

    /// <summary>Optional keyword matched against the role title.</summary>
    public string? Keyword { get; private set; }

    /// <summary>Whether job-alert digests are enabled for this search.</summary>
    public bool AlertsEnabled { get; private set; }

    /// <summary>When the search was saved (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a saved search, enforcing a non-empty label and talent.</summary>
    /// <param name="talentId">Owning talent id (required).</param>
    /// <param name="label">Label (required).</param>
    /// <param name="city">Optional city filter.</param>
    /// <param name="keyword">Optional title keyword.</param>
    /// <param name="alertsEnabled">Whether alerts are enabled.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The saved search, or a validation error.</returns>
    public static Result<SavedSearch> Create(Guid talentId, string label, string? city, string? keyword, bool alertsEnabled, DateTimeOffset createdAt)
    {
        if (talentId == Guid.Empty)
        {
            return Error.Validation("saved_search.talent_required", "A talent id is required.");
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return Error.Validation("saved_search.label_required", "A label is required.");
        }

        return new SavedSearch(SavedSearchId.New())
        {
            TalentId = talentId,
            Label = label.Trim(),
            City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            Keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim(),
            AlertsEnabled = alertsEnabled,
            CreatedAt = createdAt,
        };
    }

    /// <summary>Enables or disables job-alert digests for this search.</summary>
    /// <param name="enabled">Whether alerts should be enabled.</param>
    public void SetAlerts(bool enabled) => AlertsEnabled = enabled;
}
