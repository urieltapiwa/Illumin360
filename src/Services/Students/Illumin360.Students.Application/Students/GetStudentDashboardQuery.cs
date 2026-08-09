using Illumin360.SharedKernel;
using Illumin360.Students.Application.Abstractions;
using Illumin360.Students.Domain;

namespace Illumin360.Students.Application.Students;

/// <summary>
/// Query for a student's dashboard. When <see cref="Id"/> is <see langword="null"/> the default
/// (demo) student is returned — the portal's "me" view before real per-user identity is wired.
/// </summary>
/// <param name="Id">The student id, or <see langword="null"/> for the default student.</param>
public sealed record GetStudentDashboardQuery(Guid? Id = null) : IQuery<StudentDashboardDto>;

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

        var dashboard = query.Id is { } id
            ? await _repository.GetDashboardAsync(new StudentId(id), cancellationToken).ConfigureAwait(false)
            : await _repository.GetDefaultDashboardAsync(cancellationToken).ConfigureAwait(false);

        if (dashboard is null)
        {
            return Error.NotFound("student.not_found", "No matching student was found.");
        }

        return StudentDashboardDto.FromDomain(dashboard);
    }
}
