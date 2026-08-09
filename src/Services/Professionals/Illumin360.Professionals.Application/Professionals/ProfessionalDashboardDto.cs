using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Domain;

namespace Illumin360.Professionals.Application.Professionals;

/// <summary>The professional's career persona.</summary>
/// <param name="Name">Full name.</param>
/// <param name="Role">Headline role.</param>
/// <param name="City">Home city.</param>
/// <param name="Nationality">Nationality.</param>
/// <param name="Availability">Availability label.</param>
/// <param name="Headline">Public headline.</param>
/// <param name="ProfileStrength">Profile-strength score (0–100).</param>
/// <param name="Percentile">Ranking percentile.</param>
/// <param name="MemberSince">Membership start year.</param>
public sealed record PersonaDto(
    string Name,
    string Role,
    string City,
    string Nationality,
    string Availability,
    string Headline,
    int ProfileStrength,
    int Percentile,
    string MemberSince);

/// <summary>Headline KPIs shown on the professional dashboard.</summary>
/// <param name="ProfileViews">Total profile views.</param>
/// <param name="ViewsDelta">Percentage change in views.</param>
/// <param name="MatchOpportunities">Open match opportunities.</param>
/// <param name="MatchDelta">Percentage change in matches.</param>
/// <param name="ActiveApplications">Active applications.</param>
/// <param name="ResponseRate">Response rate (percentage).</param>
/// <param name="AvgMatch">Average match score (percentage).</param>
/// <param name="Interviews">Interviews secured.</param>
public sealed record KpisDto(
    int ProfileViews,
    int ViewsDelta,
    int MatchOpportunities,
    int MatchDelta,
    int ActiveApplications,
    int ResponseRate,
    int AvgMatch,
    int Interviews);

/// <summary>A job match.</summary>
/// <param name="Role">Role title.</param>
/// <param name="Company">Hiring company.</param>
/// <param name="City">Role city.</param>
/// <param name="Industry">Company industry.</param>
/// <param name="Match">Match percentage (0–100).</param>
/// <param name="SalaryLo">Lower salary bound (NAD).</param>
/// <param name="SalaryHi">Upper salary bound (NAD).</param>
/// <param name="Type">Engagement type.</param>
/// <param name="Posted">Relative posted-time label.</param>
/// <param name="Id">Match id (for actions).</param>
/// <param name="Status">Disposition: new/saved/dismissed/applied.</param>
public sealed record MatchDto(
    string Role,
    string Company,
    string City,
    string Industry,
    int Match,
    int SalaryLo,
    int SalaryHi,
    string Type,
    string Posted,
    Guid Id,
    string Status);

/// <summary>A pipeline stage and its count.</summary>
/// <param name="Stage">Stage name.</param>
/// <param name="Value">Count at this stage.</param>
public sealed record PipelineDto(string Stage, int Value);

/// <summary>An in-demand role and its demand value.</summary>
/// <param name="Role">Role in demand.</param>
/// <param name="Value">Demand count.</param>
public sealed record SkillDemandDto(string Role, int Value);

/// <summary>A skill, proficiency and market trend.</summary>
/// <param name="Name">Skill name.</param>
/// <param name="Level">Proficiency (0–100).</param>
/// <param name="Trend">Market trend tag.</param>
public sealed record SkillDto(string Name, int Level, string Trend);

/// <summary>Salary benchmark for the professional's role.</summary>
/// <param name="Role">Benchmark role.</param>
/// <param name="P25">25th-percentile salary (NAD).</param>
/// <param name="Median">Median salary (NAD).</param>
/// <param name="P75">75th-percentile salary (NAD).</param>
/// <param name="You">This professional's salary (NAD).</param>
public sealed record SalaryDto(string Role, int P25, int Median, int P75, int You);

/// <summary>An activity-feed entry.</summary>
/// <param name="Icon">Icon key.</param>
/// <param name="Text">Activity text.</param>
/// <param name="When">Relative time label.</param>
public sealed record ActivityDto(string Icon, string Text, string When);

/// <summary>Full dashboard payload for a professional (matches the Professional portal's data contract).</summary>
/// <param name="Id">The professional's id.</param>
/// <param name="Persona">Career persona.</param>
/// <param name="Kpis">Headline KPIs.</param>
/// <param name="ViewsTrend">Profile-view trend, oldest first.</param>
/// <param name="Matches">Job matches.</param>
/// <param name="Pipeline">Application pipeline.</param>
/// <param name="SkillDemand">In-demand roles.</param>
/// <param name="Skills">Skills.</param>
/// <param name="Salary">Salary benchmark.</param>
/// <param name="Activity">Activity feed.</param>
public sealed record ProfessionalDashboardDto(
    Guid Id,
    PersonaDto Persona,
    KpisDto Kpis,
    IReadOnlyList<int> ViewsTrend,
    IReadOnlyList<MatchDto> Matches,
    IReadOnlyList<PipelineDto> Pipeline,
    IReadOnlyList<SkillDemandDto> SkillDemand,
    IReadOnlyList<SkillDto> Skills,
    SalaryDto Salary,
    IReadOnlyList<ActivityDto> Activity)
{
    /// <summary>Projects an aggregated <see cref="ProfessionalDashboard"/> into the transport DTO.</summary>
    /// <param name="d">The aggregated dashboard read model.</param>
    /// <returns>The transport DTO.</returns>
    public static ProfessionalDashboardDto FromDomain(ProfessionalDashboard d)
    {
        ArgumentNullException.ThrowIfNull(d);
        var p = d.Professional;

        return new ProfessionalDashboardDto(
            p.Id.Value,
            new PersonaDto(
                p.FullName,
                p.Role,
                p.City,
                p.Nationality,
                p.Availability,
                p.Headline,
                p.ProfileStrength,
                p.Percentile,
                p.MemberSince),
            new KpisDto(
                p.ProfileViews,
                p.ViewsDelta,
                p.MatchOpportunities,
                p.MatchDelta,
                p.ActiveApplications,
                p.ResponseRate,
                p.AvgMatch,
                p.Interviews),
            p.ViewsTrend,
            [.. d.Matches.Select(m => new MatchDto(m.Role, m.Company, m.City, m.Industry, m.MatchScore, m.SalaryLo, m.SalaryHi, m.Type, m.PostedLabel, m.Id, m.Status switch { MatchStatus.Saved => "saved", MatchStatus.Dismissed => "dismissed", MatchStatus.Applied => "applied", _ => "new" }))],
            [.. d.Pipeline.Select(s => new PipelineDto(s.Stage, s.Value))],
            [.. d.SkillDemand.Select(s => new SkillDemandDto(s.Role, s.Value))],
            [.. d.Skills.Select(s => new SkillDto(s.Name, s.Level, s.Trend))],
            new SalaryDto(p.SalaryRole, p.SalaryP25, p.SalaryMedian, p.SalaryP75, p.SalaryYou),
            [.. d.Activity.Select(a => new ActivityDto(a.Icon, a.Text, a.WhenLabel))]);
    }
}
