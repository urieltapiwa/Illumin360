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

    /// <summary>Commits pending changes (and flushes the outbox in the same transaction).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
