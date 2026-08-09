using Illumin360.Students.Application.Abstractions;
using Illumin360.Students.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Students.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IStudentRepository"/>.</summary>
/// <param name="db">The Students database context.</param>
public sealed class StudentRepository(StudentsDbContext db) : IStudentRepository
{
    private readonly StudentsDbContext _db = db;

    /// <inheritdoc />
    public async Task<StudentDashboard?> GetDashboardAsync(StudentId id, CancellationToken cancellationToken)
    {
        var student = await _db.Students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);

        return student is null ? null : await LoadAsync(student, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StudentDashboard?> GetDefaultDashboardAsync(CancellationToken cancellationToken)
    {
        var student = await _db.Students.AsNoTracking()
            .OrderBy(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return student is null ? null : await LoadAsync(student, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Add(Student student) => _db.Students.Add(student);

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);

    private async Task<StudentDashboard> LoadAsync(Student student, CancellationToken cancellationToken)
    {
        var id = student.Id;

        var skills = await _db.Skills.AsNoTracking()
            .Where(x => x.StudentId == id).OrderBy(x => x.Sort)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var learning = await _db.Learning.AsNoTracking()
            .Where(x => x.StudentId == id).OrderBy(x => x.Sort)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var matches = await _db.Matches.AsNoTracking()
            .Where(x => x.StudentId == id).OrderBy(x => x.Sort)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var pipeline = await _db.Pipeline.AsNoTracking()
            .Where(x => x.StudentId == id).OrderBy(x => x.Sort)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var activity = await _db.Activity.AsNoTracking()
            .Where(x => x.StudentId == id).OrderBy(x => x.Sort)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new StudentDashboard(student, skills, learning, matches, pipeline, activity);
    }
}
