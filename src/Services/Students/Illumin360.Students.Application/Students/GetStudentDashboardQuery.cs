using Illumin360.SharedKernel;
using Illumin360.Students.Application.Abstractions;
using Illumin360.Students.Domain;

namespace Illumin360.Students.Application.Students;

/// <summary>
/// Query for a student's dashboard. When <see cref="Id"/> and <see cref="Subject"/> are both
/// <see langword="null"/> the default (demo) student is returned; otherwise the profile is resolved
/// by id or by the caller's Keycloak subject (falling back to demo when no profile is linked).
/// </summary>
/// <param name="Id">The student id, or <see langword="null"/> to resolve by subject / default.</param>
/// <param name="Subject">The caller's Keycloak subject (user id), or <see langword="null"/>.</param>
public sealed record GetStudentDashboardQuery(Guid? Id = null, string? Subject = null) : IQuery<StudentDashboardDto>;

/// <summary>Handles <see cref="GetStudentDashboardQuery"/>.</summary>
/// <param name="repository">The student repository.</param>
public sealed class GetStudentDashboardQueryHandler(IStudentRepository repository)
    : IQueryHandler<GetStudentDashboardQuery, StudentDashboardDto>
{
    private readonly IStudentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<StudentDashboardDto>> HandleAsync(
        GetStudentDashboardQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        StudentDashboard? dashboard;
        if (query.Id is { } id)
        {
            dashboard = await _repository.GetDashboardAsync(new StudentId(id), cancellationToken).ConfigureAwait(false);
        }
        else if (!string.IsNullOrWhiteSpace(query.Subject))
        {
            // Per-user identity: resolve the caller's own profile by their Keycloak subject.
            // Fall back to the demo profile so seeded logins (no linked profile) still see a dashboard.
            dashboard = await _repository.GetDashboardBySubjectAsync(query.Subject, cancellationToken).ConfigureAwait(false)
                ?? await _repository.GetDefaultDashboardAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            dashboard = await _repository.GetDefaultDashboardAsync(cancellationToken).ConfigureAwait(false);
        }

        if (dashboard is null)
        {
            return Error.NotFound("student.not_found", "No matching student was found.");
        }

        return StudentDashboardDto.FromDomain(dashboard);
    }
}
