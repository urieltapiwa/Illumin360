using Illumin360.Students.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Students.Infrastructure.Persistence;

/// <summary>
/// Seeds the Students database with a demo cohort on first run (idempotent). Real cohorts arrive via
/// registration + data import; this gives the Student portal live, Postgres-backed data out of the box.
/// </summary>
public static class StudentsSeeder
{
    private static readonly Guid DemoStudentId = new("5e1a0000-0000-4000-8000-000000000001");

    /// <summary>Inserts the demo cohort if the database has no students yet.</summary>
    /// <param name="db">The Students database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when seeding is done (or skipped).</returns>
    public static async Task SeedAsync(StudentsDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (await db.Students.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var id = new StudentId(DemoStudentId);
        var createdAt = new DateTimeOffset(2023, 2, 1, 0, 0, 0, TimeSpan.Zero);

        var student = Student.Seed(
            DemoStudentId,
            firstName: "Selma",
            lastName: "Nghidinwa",
            field: "Computer Science",
            school: "Namibia Univ. of Science & Technology",
            year: "Final year",
            graduating: "2026",
            program: "Illumin Futures (CSR)",
            city: "Windhoek",
            readiness: 78,
            profileViews: 76,
            viewsDelta: 31,
            mentorSessions: 3,
            applicationsCount: 4,
            viewsTrend: [4, 6, 5, 8, 11, 14, 12, 18, 22, 27, 33, 41, 58, 76],
            createdAt: createdAt);

        db.Students.Add(student);

        db.Skills.AddRange(
            new StudentSkill(Guid.NewGuid(), id, "Python", 80, 0),
            new StudentSkill(Guid.NewGuid(), id, "Java", 72, 1),
            new StudentSkill(Guid.NewGuid(), id, "Web (HTML/CSS/JS)", 68, 2),
            new StudentSkill(Guid.NewGuid(), id, "Databases", 60, 3),
            new StudentSkill(Guid.NewGuid(), id, "Problem solving", 85, 4));

        db.Learning.AddRange(
            new StudentLearning(Guid.NewGuid(), id, "CV optimisation", 100, "done", 0),
            new StudentLearning(Guid.NewGuid(), id, "Interview readiness", 80, "in progress", 1),
            new StudentLearning(Guid.NewGuid(), id, "SQL fundamentals", 60, "in progress", 2),
            new StudentLearning(Guid.NewGuid(), id, "Git & collaboration", 40, "in progress", 3),
            new StudentLearning(Guid.NewGuid(), id, "Professional communication", 100, "done", 4));

        db.Matches.AddRange(
            new StudentMatch(Guid.NewGuid(), id, "IT Intern", "Unity Group", "Walvis Bay", 95, 9000, 16000, "Internship", "1d", 0),
            new StudentMatch(Guid.NewGuid(), id, "Graduate Software Developer", "Baobab (Pty) Ltd", "Rundu", 94, 9000, 16000, "Internship", "3d", 1),
            new StudentMatch(Guid.NewGuid(), id, "Junior Data Analyst", "Kalahari CC", "Windhoek", 94, 8000, 12000, "Internship", "1d", 2),
            new StudentMatch(Guid.NewGuid(), id, "Trainee Network Engineer", "Atlantic Trading", "Windhoek", 92, 8000, 16000, "Internship", "3d", 3),
            new StudentMatch(Guid.NewGuid(), id, "Software Developer Intern", "Zambezi Consulting", "Windhoek", 86, 6000, 16000, "Internship", "1w", 4),
            new StudentMatch(Guid.NewGuid(), id, "Graduate Accountant", "Cornerstone Trading", "Windhoek", 80, 9000, 12000, "Internship", "1d", 5));

        db.Pipeline.AddRange(
            new StudentPipelineStage(Guid.NewGuid(), id, "Applied", 9, 0),
            new StudentPipelineStage(Guid.NewGuid(), id, "Reviewed", 6, 1),
            new StudentPipelineStage(Guid.NewGuid(), id, "Interview", 2, 2),
            new StudentPipelineStage(Guid.NewGuid(), id, "Offer", 1, 3));

        db.Activity.AddRange(
            new StudentActivity(Guid.NewGuid(), id, "Mentor session booked with a Senior Engineer", "1h ago", 0),
            new StudentActivity(Guid.NewGuid(), id, "New internship match · IT Intern at Erongo Tech (92%)", "4h ago", 1),
            new StudentActivity(Guid.NewGuid(), id, "You completed 'CV optimisation' module", "1d ago", 2),
            new StudentActivity(Guid.NewGuid(), id, "Namib Mills viewed your student profile", "2d ago", 3),
            new StudentActivity(Guid.NewGuid(), id, "Readiness score increased to 78%", "3d ago", 4));

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
