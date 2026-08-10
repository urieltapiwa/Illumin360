using Illumin360.SharedKernel;

namespace Illumin360.Students.Domain;

/// <summary>The student's disposition toward a surfaced internship/graduate match.</summary>
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

/// <summary>A self-assessed or verified skill and its proficiency level for a student.</summary>
public sealed class StudentSkill : Entity<Guid>
{
    private StudentSkill(Guid id)
        : base(id)
    {
    }

    /// <summary>Creates a skill row.</summary>
    /// <param name="id">Row identity.</param>
    /// <param name="studentId">Owning student.</param>
    /// <param name="name">Skill name.</param>
    /// <param name="level">Proficiency (0–100).</param>
    /// <param name="sort">Display order.</param>
    public StudentSkill(Guid id, StudentId studentId, string name, int level, int sort)
        : base(id)
    {
        StudentId = studentId;
        Name = name;
        Level = level;
        Sort = sort;
    }

    /// <summary>Owning student.</summary>
    public StudentId StudentId { get; private set; }

    /// <summary>Skill name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Proficiency (0–100).</summary>
    public int Level { get; private set; }

    /// <summary>Display order.</summary>
    public int Sort { get; private set; }
}

/// <summary>A learning module the student is working through, with completion progress.</summary>
public sealed class StudentLearning : Entity<Guid>
{
    private StudentLearning(Guid id)
        : base(id)
    {
    }

    /// <summary>Creates a learning-module row.</summary>
    /// <param name="id">Row identity.</param>
    /// <param name="studentId">Owning student.</param>
    /// <param name="name">Module name.</param>
    /// <param name="progress">Completion percentage (0–100).</param>
    /// <param name="tag">Status tag (e.g. "done", "in progress").</param>
    /// <param name="sort">Display order.</param>
    public StudentLearning(Guid id, StudentId studentId, string name, int progress, string tag, int sort)
        : base(id)
    {
        StudentId = studentId;
        Name = name;
        Progress = progress;
        Tag = tag;
        Sort = sort;
    }

    /// <summary>Owning student.</summary>
    public StudentId StudentId { get; private set; }

    /// <summary>Module name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Completion percentage (0–100).</summary>
    public int Progress { get; private set; }

    /// <summary>Status tag (e.g. "done", "in progress").</summary>
    public string Tag { get; private set; } = string.Empty;

    /// <summary>Display order.</summary>
    public int Sort { get; private set; }
}

/// <summary>An internship / graduate-role match surfaced to the student.</summary>
public sealed class StudentMatch : Entity<Guid>
{
    private StudentMatch(Guid id)
        : base(id)
    {
    }

    /// <summary>Creates a match row.</summary>
    /// <param name="id">Row identity.</param>
    /// <param name="studentId">Owning student.</param>
    /// <param name="role">Role title.</param>
    /// <param name="company">Hiring company.</param>
    /// <param name="city">Role city.</param>
    /// <param name="matchScore">Match percentage (0–100).</param>
    /// <param name="stipendLo">Lower stipend bound (NAD).</param>
    /// <param name="stipendHi">Upper stipend bound (NAD).</param>
    /// <param name="type">Engagement type (e.g. "Internship").</param>
    /// <param name="postedLabel">Relative posted-time label (e.g. "1d").</param>
    /// <param name="sort">Display order.</param>
    public StudentMatch(
        Guid id,
        StudentId studentId,
        string role,
        string company,
        string city,
        int matchScore,
        int stipendLo,
        int stipendHi,
        string type,
        string postedLabel,
        int sort)
        : base(id)
    {
        StudentId = studentId;
        Role = role;
        Company = company;
        City = city;
        MatchScore = matchScore;
        StipendLo = stipendLo;
        StipendHi = stipendHi;
        Type = type;
        PostedLabel = postedLabel;
        Sort = sort;
    }

    /// <summary>Owning student.</summary>
    public StudentId StudentId { get; private set; }

    /// <summary>Role title.</summary>
    public string Role { get; private set; } = string.Empty;

    /// <summary>Hiring company.</summary>
    public string Company { get; private set; } = string.Empty;

    /// <summary>Role city.</summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>Match percentage (0–100).</summary>
    public int MatchScore { get; private set; }

    /// <summary>Lower stipend bound (NAD).</summary>
    public int StipendLo { get; private set; }

    /// <summary>Upper stipend bound (NAD).</summary>
    public int StipendHi { get; private set; }

    /// <summary>Engagement type (e.g. "Internship").</summary>
    public string Type { get; private set; } = string.Empty;

    /// <summary>Relative posted-time label (e.g. "1d").</summary>
    public string PostedLabel { get; private set; } = string.Empty;

    /// <summary>Display order.</summary>
    public int Sort { get; private set; }

    /// <summary>The student's disposition toward this match.</summary>
    public MatchStatus Status { get; private set; }

    /// <summary>Marks the match as saved.</summary>
    public void Save() => Status = MatchStatus.Saved;

    /// <summary>Marks the match as dismissed.</summary>
    public void Dismiss() => Status = MatchStatus.Dismissed;

    /// <summary>Marks the match as applied to.</summary>
    /// <returns>Success, or a conflict if it was already applied.</returns>
    public Result<StudentMatch> Apply()
    {
        if (Status == MatchStatus.Applied)
        {
            return Error.Conflict("match.already_applied", "You have already applied to this match.");
        }

        Status = MatchStatus.Applied;
        return this;
    }
}

/// <summary>A stage in the student's application pipeline with the count at that stage.</summary>
public sealed class StudentPipelineStage : Entity<Guid>
{
    private StudentPipelineStage(Guid id)
        : base(id)
    {
    }

    /// <summary>Creates a pipeline-stage row.</summary>
    /// <param name="id">Row identity.</param>
    /// <param name="studentId">Owning student.</param>
    /// <param name="stage">Stage name (e.g. "Applied").</param>
    /// <param name="value">Count at this stage.</param>
    /// <param name="sort">Display order (funnel order).</param>
    public StudentPipelineStage(Guid id, StudentId studentId, string stage, int value, int sort)
        : base(id)
    {
        StudentId = studentId;
        Stage = stage;
        Value = value;
        Sort = sort;
    }

    /// <summary>Owning student.</summary>
    public StudentId StudentId { get; private set; }

    /// <summary>Stage name (e.g. "Applied").</summary>
    public string Stage { get; private set; } = string.Empty;

    /// <summary>Count at this stage.</summary>
    public int Value { get; private set; }

    /// <summary>Display order (funnel order).</summary>
    public int Sort { get; private set; }
}

/// <summary>An activity-feed entry for the student.</summary>
public sealed class StudentActivity : Entity<Guid>
{
    private StudentActivity(Guid id)
        : base(id)
    {
    }

    /// <summary>Creates an activity row.</summary>
    /// <param name="id">Row identity.</param>
    /// <param name="studentId">Owning student.</param>
    /// <param name="text">Activity text.</param>
    /// <param name="whenLabel">Relative time label (e.g. "1h ago").</param>
    /// <param name="sort">Display order (newest first).</param>
    public StudentActivity(Guid id, StudentId studentId, string text, string whenLabel, int sort)
        : base(id)
    {
        StudentId = studentId;
        Text = text;
        WhenLabel = whenLabel;
        Sort = sort;
    }

    /// <summary>Owning student.</summary>
    public StudentId StudentId { get; private set; }

    /// <summary>Activity text.</summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>Relative time label (e.g. "1h ago").</summary>
    public string WhenLabel { get; private set; } = string.Empty;

    /// <summary>Display order (newest first).</summary>
    public int Sort { get; private set; }
}
