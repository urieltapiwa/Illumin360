using Illumin360.Students.Application.Abstractions;

namespace Illumin360.Students.Application.Students;

/// <summary>The student's academic persona.</summary>
/// <param name="Name">Full name.</param>
/// <param name="Field">Field of study.</param>
/// <param name="School">Institution.</param>
/// <param name="Year">Year-of-study label.</param>
/// <param name="Graduating">Expected graduation year label.</param>
/// <param name="Readiness">Career-readiness score (0–100).</param>
/// <param name="Program">Sponsoring programme.</param>
/// <param name="City">Home city.</param>
public sealed record PersonaDto(
    string Name,
    string Field,
    string School,
    string Year,
    string Graduating,
    int Readiness,
    string Program,
    string City);

/// <summary>Headline KPIs shown on the student dashboard.</summary>
/// <param name="ProfileViews">Total profile views.</param>
/// <param name="ViewsDelta">Percentage change in views.</param>
/// <param name="InternshipMatches">Number of open matches.</param>
/// <param name="Applications">Applications submitted.</param>
/// <param name="SkillsDone">Learning modules completed.</param>
/// <param name="MentorSessions">Mentor sessions attended.</param>
/// <param name="Readiness">Career-readiness score (0–100).</param>
public sealed record KpisDto(
    int ProfileViews,
    int ViewsDelta,
    int InternshipMatches,
    int Applications,
    int SkillsDone,
    int MentorSessions,
    int Readiness);

/// <summary>An internship/graduate-role match.</summary>
/// <param name="Role">Role title.</param>
/// <param name="Company">Hiring company.</param>
/// <param name="City">Role city.</param>
/// <param name="Match">Match percentage (0–100).</param>
/// <param name="StipendLo">Lower stipend bound (NAD).</param>
/// <param name="StipendHi">Upper stipend bound (NAD).</param>
/// <param name="Type">Engagement type.</param>
/// <param name="Posted">Relative posted-time label.</param>
public sealed record MatchDto(
    string Role,
    string Company,
    string City,
    int Match,
    int StipendLo,
    int StipendHi,
    string Type,
    string Posted);

/// <summary>A learning module with completion progress.</summary>
/// <param name="Name">Module name.</param>
/// <param name="Progress">Completion percentage (0–100).</param>
/// <param name="Tag">Status tag.</param>
public sealed record LearningDto(string Name, int Progress, string Tag);

/// <summary>A pipeline stage and its count.</summary>
/// <param name="Stage">Stage name.</param>
/// <param name="Value">Count at this stage.</param>
public sealed record PipelineDto(string Stage, int Value);

/// <summary>A skill and its proficiency level.</summary>
/// <param name="Name">Skill name.</param>
/// <param name="Level">Proficiency (0–100).</param>
public sealed record SkillDto(string Name, int Level);

/// <summary>An activity-feed entry.</summary>
/// <param name="Text">Activity text.</param>
/// <param name="When">Relative time label.</param>
public sealed record ActivityDto(string Text, string When);

/// <summary>Full dashboard payload for a student (matches the Student portal's data contract).</summary>
/// <param name="Id">The student's id.</param>
/// <param name="Persona">Academic persona.</param>
/// <param name="Kpis">Headline KPIs.</param>
/// <param name="ViewsTrend">Profile-view trend, oldest first.</param>
/// <param name="Matches">Internship/graduate matches.</param>
/// <param name="Learning">Learning modules.</param>
/// <param name="Pipeline">Application pipeline.</param>
/// <param name="Skills">Skills.</param>
/// <param name="Activity">Activity feed.</param>
public sealed record StudentDashboardDto(
    Guid Id,
    PersonaDto Persona,
    KpisDto Kpis,
    IReadOnlyList<int> ViewsTrend,
    IReadOnlyList<MatchDto> Matches,
    IReadOnlyList<LearningDto> Learning,
    IReadOnlyList<PipelineDto> Pipeline,
    IReadOnlyList<SkillDto> Skills,
    IReadOnlyList<ActivityDto> Activity)
{
    /// <summary>Projects an aggregated <see cref="StudentDashboard"/> into the transport DTO.</summary>
    /// <param name="d">The aggregated dashboard read model.</param>
    /// <returns>The transport DTO.</returns>
    public static StudentDashboardDto FromDomain(StudentDashboard d)
    {
        ArgumentNullException.ThrowIfNull(d);
        var s = d.Student;
        var skillsDone = d.Learning.Count(l => string.Equals(l.Tag, "done", StringComparison.OrdinalIgnoreCase));

        return new StudentDashboardDto(
            s.Id.Value,
            new PersonaDto(s.FullName, s.Field, s.School, s.Year, s.Graduating, s.Readiness, s.Program, s.City),
            new KpisDto(
                s.ProfileViews,
                s.ViewsDelta,
                d.Matches.Count,
                s.ApplicationsCount,
                skillsDone,
                s.MentorSessions,
                s.Readiness),
            s.ViewsTrend,
            [.. d.Matches.Select(m => new MatchDto(
                m.Role, m.Company, m.City, m.MatchScore, m.StipendLo, m.StipendHi, m.Type, m.PostedLabel))],
            [.. d.Learning.Select(l => new LearningDto(l.Name, l.Progress, l.Tag))],
            [.. d.Pipeline.Select(p => new PipelineDto(p.Stage, p.Value))],
            [.. d.Skills.Select(sk => new SkillDto(sk.Name, sk.Level))],
            [.. d.Activity.Select(a => new ActivityDto(a.Text, a.WhenLabel))]);
    }
}
