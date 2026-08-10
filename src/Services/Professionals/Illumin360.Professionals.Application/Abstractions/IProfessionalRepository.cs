using Illumin360.Professionals.Domain;

namespace Illumin360.Professionals.Application.Abstractions;

/// <summary>Aggregated read model for a single professional's dashboard.</summary>
/// <param name="Professional">The professional aggregate.</param>
/// <param name="Matches">Job matches, in display order.</param>
/// <param name="Pipeline">Application-pipeline stages, in funnel order.</param>
/// <param name="SkillDemand">In-demand roles, in display order.</param>
/// <param name="Skills">Skills, in display order.</param>
/// <param name="Activity">Activity feed, newest first.</param>
public sealed record ProfessionalDashboard(
    Professional Professional,
    IReadOnlyList<ProfessionalMatch> Matches,
    IReadOnlyList<ProfessionalPipelineStage> Pipeline,
    IReadOnlyList<ProfessionalSkillDemand> SkillDemand,
    IReadOnlyList<ProfessionalSkill> Skills,
    IReadOnlyList<ProfessionalActivity> Activity);

/// <summary>Persistence port for the Professionals bounded context.</summary>
public interface IProfessionalRepository
{
    /// <summary>Loads the full dashboard for a professional by id.</summary>
    /// <param name="id">The professional id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dashboard, or <see langword="null"/> if the professional does not exist.</returns>
    Task<ProfessionalDashboard?> GetDashboardAsync(ProfessionalId id, CancellationToken cancellationToken);

    /// <summary>Loads the dashboard for the default (most recently created) demo professional.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dashboard, or <see langword="null"/> if no professionals exist.</returns>
    Task<ProfessionalDashboard?> GetDefaultDashboardAsync(CancellationToken cancellationToken);

    /// <summary>Stages a new professional for insertion.</summary>
    /// <param name="professional">The professional to add.</param>
    void Add(Professional professional);

    /// <summary>The default ("me") professional's id (most recently created), or null if none exist.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default professional id, or null.</returns>
    Task<ProfessionalId?> GetDefaultProfessionalIdAsync(CancellationToken cancellationToken);

    /// <summary>Loads a professional for update (change-tracked).</summary>
    /// <param name="id">The professional id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tracked professional, or null.</returns>
    Task<Professional?> GetTrackedAsync(ProfessionalId id, CancellationToken cancellationToken);

    /// <summary>Loads a match belonging to a professional for update (change-tracked).</summary>
    /// <param name="professionalId">Owning professional.</param>
    /// <param name="matchId">Match id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tracked match, or null.</returns>
    Task<ProfessionalMatch?> GetMatchAsync(ProfessionalId professionalId, Guid matchId, CancellationToken cancellationToken);

    /// <summary>Returns the professional's current skill names.</summary>
    /// <param name="professionalId">Owning professional.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The skill names.</returns>
    Task<IReadOnlyList<string>> GetSkillNamesAsync(ProfessionalId professionalId, CancellationToken cancellationToken);

    /// <summary>Stages a new skill for insertion. Persisted on <see cref="SaveChangesAsync"/>.</summary>
    /// <param name="skill">The skill to add.</param>
    void AddSkill(ProfessionalSkill skill);

    /// <summary>Stages a new in-app notification for insertion.</summary>
    /// <param name="notification">The notification to add.</param>
    void AddNotification(ProfessionalNotification notification);

    /// <summary>Lists a professional's notifications, newest first.</summary>
    /// <param name="professionalId">Recipient professional.</param>
    /// <param name="unreadOnly">Whether to return only unread notifications.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ProfessionalNotification>> ListNotificationsAsync(ProfessionalId professionalId, bool unreadOnly, CancellationToken cancellationToken);

    /// <summary>Loads a notification for update, or null if not present.</summary>
    /// <param name="id">Notification id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ProfessionalNotification?> GetNotificationAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Marks all of a professional's notifications read; returns the count updated.</summary>
    /// <param name="professionalId">Recipient professional.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<int> MarkAllNotificationsReadAsync(ProfessionalId professionalId, CancellationToken cancellationToken);

    /// <summary>Commits pending changes (and flushes the outbox in the same transaction).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
