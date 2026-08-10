using Illumin360.SharedKernel;

namespace Illumin360.Professionals.Domain;

/// <summary>The professional's disposition toward a surfaced match.</summary>
public enum MatchStatus
{
    /// <summary>Newly surfaced, no action taken.</summary>
    New,

    /// <summary>Saved for later.</summary>
    Saved,

    /// <summary>Dismissed / not interested.</summary>
    Dismissed,

    /// <summary>Applied to.</summary>
    Applied,
}

/// <summary>A job match surfaced to the professional.</summary>
public sealed class ProfessionalMatch : Entity<Guid>
{
    private ProfessionalMatch(Guid id)
        : base(id)
    {
    }

    /// <summary>Creates a match row.</summary>
    /// <param name="id">Row identity.</param>
    /// <param name="professionalId">Owning professional.</param>
    /// <param name="role">Role title.</param>
    /// <param name="company">Hiring company.</param>
    /// <param name="city">Role city.</param>
    /// <param name="industry">Company industry.</param>
    /// <param name="matchScore">Match percentage (0–100).</param>
    /// <param name="salaryLo">Lower salary bound (NAD).</param>
    /// <param name="salaryHi">Upper salary bound (NAD).</param>
    /// <param name="type">Engagement type (e.g. "Full-time").</param>
    /// <param name="postedLabel">Relative posted-time label.</param>
    /// <param name="sort">Display order.</param>
    public ProfessionalMatch(
        Guid id,
        ProfessionalId professionalId,
        string role,
        string company,
        string city,
        string industry,
        int matchScore,
        int salaryLo,
        int salaryHi,
        string type,
        string postedLabel,
        int sort)
        : base(id)
    {
        ProfessionalId = professionalId;
        Role = role;
        Company = company;
        City = city;
        Industry = industry;
        MatchScore = matchScore;
        SalaryLo = salaryLo;
        SalaryHi = salaryHi;
        Type = type;
        PostedLabel = postedLabel;
        Sort = sort;
    }

    /// <summary>Owning professional.</summary>
    public ProfessionalId ProfessionalId { get; private set; }

    /// <summary>Role title.</summary>
    public string Role { get; private set; } = string.Empty;

    /// <summary>Hiring company.</summary>
    public string Company { get; private set; } = string.Empty;

    /// <summary>Role city.</summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>Company industry.</summary>
    public string Industry { get; private set; } = string.Empty;

    /// <summary>Match percentage (0–100).</summary>
    public int MatchScore { get; private set; }

    /// <summary>Lower salary bound (NAD).</summary>
    public int SalaryLo { get; private set; }

    /// <summary>Upper salary bound (NAD).</summary>
    public int SalaryHi { get; private set; }

    /// <summary>Engagement type.</summary>
    public string Type { get; private set; } = string.Empty;

    /// <summary>Relative posted-time label.</summary>
    public string PostedLabel { get; private set; } = string.Empty;

    /// <summary>Display order.</summary>
    public int Sort { get; private set; }

    /// <summary>The professional's disposition toward this match.</summary>
    public MatchStatus Status { get; private set; }

    /// <summary>Marks the match as saved.</summary>
    public void Save() => Status = MatchStatus.Saved;

    /// <summary>Marks the match as dismissed.</summary>
    public void Dismiss() => Status = MatchStatus.Dismissed;

    /// <summary>Marks the match as applied to.</summary>
    /// <returns>Success, or a conflict if it was already applied.</returns>
    public Result<ProfessionalMatch> Apply()
    {
        if (Status == MatchStatus.Applied)
        {
            return Error.Conflict("match.already_applied", "You have already applied to this match.");
        }

        Status = MatchStatus.Applied;
        return this;
    }
}

/// <summary>A stage in the professional's application pipeline with the count at that stage.</summary>
public sealed class ProfessionalPipelineStage : Entity<Guid>
{
    private ProfessionalPipelineStage(Guid id)
        : base(id)
    {
    }

    /// <summary>Creates a pipeline-stage row.</summary>
    /// <param name="id">Row identity.</param>
    /// <param name="professionalId">Owning professional.</param>
    /// <param name="stage">Stage name.</param>
    /// <param name="value">Count at this stage.</param>
    /// <param name="sort">Display order (funnel order).</param>
    public ProfessionalPipelineStage(Guid id, ProfessionalId professionalId, string stage, int value, int sort)
        : base(id)
    {
        ProfessionalId = professionalId;
        Stage = stage;
        Value = value;
        Sort = sort;
    }

    /// <summary>Owning professional.</summary>
    public ProfessionalId ProfessionalId { get; private set; }

    /// <summary>Stage name.</summary>
    public string Stage { get; private set; } = string.Empty;

    /// <summary>Count at this stage.</summary>
    public int Value { get; private set; }

    /// <summary>Display order.</summary>
    public int Sort { get; private set; }
}

/// <summary>Market demand for a role (used for the in-demand-roles chart).</summary>
public sealed class ProfessionalSkillDemand : Entity<Guid>
{
    private ProfessionalSkillDemand(Guid id)
        : base(id)
    {
    }

    /// <summary>Creates a skill-demand row.</summary>
    /// <param name="id">Row identity.</param>
    /// <param name="professionalId">Owning professional.</param>
    /// <param name="role">Role in demand.</param>
    /// <param name="value">Demand count / open roles.</param>
    /// <param name="sort">Display order.</param>
    public ProfessionalSkillDemand(Guid id, ProfessionalId professionalId, string role, int value, int sort)
        : base(id)
    {
        ProfessionalId = professionalId;
        Role = role;
        Value = value;
        Sort = sort;
    }

    /// <summary>Owning professional.</summary>
    public ProfessionalId ProfessionalId { get; private set; }

    /// <summary>Role in demand.</summary>
    public string Role { get; private set; } = string.Empty;

    /// <summary>Demand count / open roles.</summary>
    public int Value { get; private set; }

    /// <summary>Display order.</summary>
    public int Sort { get; private set; }
}

/// <summary>A skill and its proficiency + market trend for a professional.</summary>
public sealed class ProfessionalSkill : Entity<Guid>
{
    private ProfessionalSkill(Guid id)
        : base(id)
    {
    }

    /// <summary>Creates a skill row.</summary>
    /// <param name="id">Row identity.</param>
    /// <param name="professionalId">Owning professional.</param>
    /// <param name="name">Skill name.</param>
    /// <param name="level">Proficiency (0–100).</param>
    /// <param name="trend">Market trend tag (e.g. "hot").</param>
    /// <param name="sort">Display order.</param>
    public ProfessionalSkill(Guid id, ProfessionalId professionalId, string name, int level, string trend, int sort)
        : base(id)
    {
        ProfessionalId = professionalId;
        Name = name;
        Level = level;
        Trend = trend;
        Sort = sort;
    }

    /// <summary>Owning professional.</summary>
    public ProfessionalId ProfessionalId { get; private set; }

    /// <summary>Skill name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Proficiency (0–100).</summary>
    public int Level { get; private set; }

    /// <summary>Market trend tag (e.g. "hot").</summary>
    public string Trend { get; private set; } = string.Empty;

    /// <summary>Display order.</summary>
    public int Sort { get; private set; }

    /// <summary>Number of endorsements this skill has received.</summary>
    public int Endorsements { get; private set; }

    /// <summary>Records one more endorsement for this skill.</summary>
    public void Endorse() => Endorsements += 1;

    /// <summary>Removes one endorsement (never below zero).</summary>
    public void Unendorse() => Endorsements = Math.Max(0, Endorsements - 1);

    /// <summary>Updates the self-assessed proficiency level.</summary>
    /// <param name="level">New proficiency (0–100).</param>
    /// <returns>Success, or a validation error if out of range.</returns>
    public Result<ProfessionalSkill> UpdateLevel(int level)
    {
        if (level is < 0 or > 100)
        {
            return Error.Validation("skill.level_invalid", "Proficiency must be between 0 and 100.");
        }

        Level = level;
        return this;
    }
}

/// <summary>An activity-feed entry for the professional.</summary>
public sealed class ProfessionalActivity : Entity<Guid>
{
    private ProfessionalActivity(Guid id)
        : base(id)
    {
    }

    /// <summary>Creates an activity row.</summary>
    /// <param name="id">Row identity.</param>
    /// <param name="professionalId">Owning professional.</param>
    /// <param name="icon">Icon key (e.g. "view").</param>
    /// <param name="text">Activity text.</param>
    /// <param name="whenLabel">Relative time label.</param>
    /// <param name="sort">Display order (newest first).</param>
    public ProfessionalActivity(Guid id, ProfessionalId professionalId, string icon, string text, string whenLabel, int sort)
        : base(id)
    {
        ProfessionalId = professionalId;
        Icon = icon;
        Text = text;
        WhenLabel = whenLabel;
        Sort = sort;
    }

    /// <summary>Owning professional.</summary>
    public ProfessionalId ProfessionalId { get; private set; }

    /// <summary>Icon key (e.g. "view").</summary>
    public string Icon { get; private set; } = string.Empty;

    /// <summary>Activity text.</summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>Relative time label.</summary>
    public string WhenLabel { get; private set; } = string.Empty;

    /// <summary>Display order (newest first).</summary>
    public int Sort { get; private set; }
}
