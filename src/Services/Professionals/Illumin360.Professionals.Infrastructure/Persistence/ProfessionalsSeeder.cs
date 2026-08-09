using Illumin360.Professionals.Domain;
using Microsoft.EntityFrameworkCore;

namespace Illumin360.Professionals.Infrastructure.Persistence;

/// <summary>
/// Seeds the Professionals database with a demo professional on first run (idempotent). Real profiles
/// arrive via registration + data import; this gives the Professional portal live, Postgres-backed data
/// out of the box.
/// </summary>
public static class ProfessionalsSeeder
{
    private static readonly Guid DemoProfessionalId = new("9a0f0000-0000-4000-8000-000000000001");

    /// <summary>Inserts the demo professional if the database has none yet.</summary>
    /// <param name="db">The Professionals database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when seeding is done (or skipped).</returns>
    public static async Task SeedAsync(ProfessionalsDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (await db.Professionals.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var id = new ProfessionalId(DemoProfessionalId);
        var createdAt = new DateTimeOffset(2019, 3, 1, 0, 0, 0, TimeSpan.Zero);

        var professional = Professional.Seed(
            DemoProfessionalId,
            firstName: "Panduleni",
            lastName: "Amukwa",
            role: "Software Developer",
            city: "Windhoek",
            nationality: "Namibian",
            availability: "Open to opportunities",
            headline: "Full-stack developer · 6 yrs · .NET / React / cloud",
            profileStrength: 86,
            percentile: 12,
            memberSince: "2019",
            profileViews: 164,
            viewsDelta: 24,
            matchOpportunities: 18,
            matchDelta: 12,
            activeApplications: 5,
            responseRate: 64,
            avgMatch: 91,
            interviews: 3,
            viewsTrend: [20, 23, 24, 27, 29, 30, 34, 38, 43, 48, 54, 62, 69, 82],
            salaryRole: "Software Developer",
            salaryP25: 32000,
            salaryMedian: 46000,
            salaryP75: 64000,
            salaryYou: 52000,
            createdAt: createdAt);

        db.Professionals.Add(professional);

        db.Matches.AddRange(
            new ProfessionalMatch(Guid.NewGuid(), id, "Senior Software Developer", "Cornerstone Consulting", "Windhoek", "Telecommunications", 95, 28000, 42000, "Full-time", "1w", 0),
            new ProfessionalMatch(Guid.NewGuid(), id, "Data Engineer", "Kunene Consulting", "Walvis Bay", "NGO & Development", 94, 52000, 80000, "Full-time", "2d", 1),
            new ProfessionalMatch(Guid.NewGuid(), id, "DevOps Engineer", "Meridian (Pty) Ltd", "Oshakati", "Mining", 86, 28000, 42000, "Contract", "1w", 2),
            new ProfessionalMatch(Guid.NewGuid(), id, "Full-Stack Developer", "Fish River CC", "Oshakati", "Agriculture", 85, 28000, 42000, "Full-time", "4d", 3),
            new ProfessionalMatch(Guid.NewGuid(), id, "Solutions Architect", "Kalahari Namibia", "Oshakati", "Healthcare", 85, 32000, 48000, "Full-time", "2w", 4),
            new ProfessionalMatch(Guid.NewGuid(), id, "Backend Engineer", "Kunene Group", "Okahandja", "Agriculture", 84, 32000, 48000, "Contract", "2w", 5));

        db.Pipeline.AddRange(
            new ProfessionalPipelineStage(Guid.NewGuid(), id, "Applied", 12, 0),
            new ProfessionalPipelineStage(Guid.NewGuid(), id, "Reviewed", 9, 1),
            new ProfessionalPipelineStage(Guid.NewGuid(), id, "Shortlisted", 5, 2),
            new ProfessionalPipelineStage(Guid.NewGuid(), id, "Interview", 3, 3),
            new ProfessionalPipelineStage(Guid.NewGuid(), id, "Offer", 1, 4));

        db.SkillDemand.AddRange(
            new ProfessionalSkillDemand(Guid.NewGuid(), id, "Software Developer", 813, 0),
            new ProfessionalSkillDemand(Guid.NewGuid(), id, "Data Analyst", 738, 1),
            new ProfessionalSkillDemand(Guid.NewGuid(), id, "Project Manager", 773, 2),
            new ProfessionalSkillDemand(Guid.NewGuid(), id, "Network Engineer", 789, 3),
            new ProfessionalSkillDemand(Guid.NewGuid(), id, "Accountant", 810, 4),
            new ProfessionalSkillDemand(Guid.NewGuid(), id, "Civil Engineer", 738, 5));

        db.Skills.AddRange(
            new ProfessionalSkill(Guid.NewGuid(), id, "C# / .NET", 92, "hot", 0),
            new ProfessionalSkill(Guid.NewGuid(), id, "React / TypeScript", 88, "hot", 1),
            new ProfessionalSkill(Guid.NewGuid(), id, "Azure / Cloud", 78, "rising", 2),
            new ProfessionalSkill(Guid.NewGuid(), id, "SQL / Postgres", 84, "steady", 3),
            new ProfessionalSkill(Guid.NewGuid(), id, "Docker / K8s", 71, "rising", 4));

        db.Activity.AddRange(
            new ProfessionalActivity(Guid.NewGuid(), id, "view", "Standard Bank Namibia viewed your profile", "2h ago", 0),
            new ProfessionalActivity(Guid.NewGuid(), id, "match", "New 94% match · Backend Engineer at Erongo Tech", "5h ago", 1),
            new ProfessionalActivity(Guid.NewGuid(), id, "shortlist", "You were shortlisted by Namib Mills", "1d ago", 2),
            new ProfessionalActivity(Guid.NewGuid(), id, "interview", "Interview scheduled · Solutions Architect", "2d ago", 3),
            new ProfessionalActivity(Guid.NewGuid(), id, "view", "12 recruiters viewed your profile this week", "3d ago", 4));

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
