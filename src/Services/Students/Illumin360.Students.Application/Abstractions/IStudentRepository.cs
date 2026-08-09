using Illumin360.Students.Domain;

namespace Illumin360.Students.Application.Abstractions;

/// <summary>Aggregated read model for a single student's dashboard.</summary>
/// <param name="Student">The student aggregate.</param>
/// <param name="Skills">Skills, in display order.</param>
/// <param name="Learning">Learning modules, in display order.</param>
/// <param name="Matches">Internship/graduate matches, in display order.</param>
/// <param name="Pipeline">Application-pipeline stages, in funnel order.</param>
/// <param name="Activity">Activity feed, newest first.</param>
public sealed record StudentDashboard(
    Student Student,
    IReadOnlyList<StudentSkill> Skills,
    IReadOnlyList<StudentLearning> Learning,
    IReadOnlyList<StudentMatch> Matches,
    IReadOnlyList<StudentPipelineStage> Pipeline,
    IReadOnlyList<StudentActivity> Activity);

/// <summary>Persistence port for the Students bounded context.</summary>
public interface IStudentRepository
{
    /// <summary>Loads the full dashboard for a student by id.</summary>
    /// <param name="id">The student id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dashboard, or <see langword="null"/> if the student does not exist.</returns>
    Task<StudentDashboard?> GetDashboardAsync(StudentId id, CancellationToken cancellationToken);

    /// <summary>Loads the dashboard for the default (most recently created) demo student.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dashboard, or <see langword="null"/> if no students exist.</returns>
    Task<StudentDashboard?> GetDefaultDashboardAsync(CancellationToken cancellationToken);

    /// <summary>Stages a new student for insertion.</summary>
    /// <param name="student">The student to add.</param>
    void Add(Student student);

    /// <summary>Commits pending changes (and flushes the outbox in the same transaction).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
