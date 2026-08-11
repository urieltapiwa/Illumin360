using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>Lifecycle of a nurture sequence.</summary>
public enum NurtureStatus
{
    /// <summary>Enrolling and sending.</summary>
    Active,

    /// <summary>Retired; no new enrolments run (terminal).</summary>
    Archived,
}

/// <summary>Lifecycle of a single recipient's journey through a sequence.</summary>
public enum EnrollmentStatus
{
    /// <summary>Progressing through steps.</summary>
    Active,

    /// <summary>Every step has been sent (terminal).</summary>
    Completed,

    /// <summary>Manually stopped before completion (terminal).</summary>
    Stopped,
}

/// <summary>
/// A multi-step nurture / drip sequence: an ordered set of <see cref="NurtureStep"/>s that recipients
/// (<see cref="NurtureEnrollment"/>) progress through over time, one email per step spaced by the step's
/// delay. Owned + migration-managed by the Recruitment service.
/// </summary>
public sealed class NurtureSequence : Entity<Guid>
{
    private NurtureSequence(Guid id)
        : base(id)
    {
    }

    /// <summary>Internal sequence name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Sequence status.</summary>
    public NurtureStatus Status { get; private set; }

    /// <summary>When the sequence was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates an active sequence.</summary>
    /// <param name="name">Internal name (required, ≤ 160 chars).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The sequence, or a validation error.</returns>
    public static Result<NurtureSequence> Create(string name, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("nurture.name_required", "A sequence name is required.");
        }

        if (name.Length > 160)
        {
            return Error.Validation("nurture.name_too_long", "The sequence name is too long.");
        }

        return new NurtureSequence(Guid.NewGuid())
        {
            Name = name.Trim(),
            Status = NurtureStatus.Active,
            CreatedAt = createdAt,
        };
    }

    /// <summary>Archives the sequence (terminal).</summary>
    public void Archive() => Status = NurtureStatus.Archived;
}

/// <summary>One email step in a <see cref="NurtureSequence"/>, sent <see cref="DelayDays"/> after the previous step.</summary>
public sealed class NurtureStep : Entity<Guid>
{
    private NurtureStep(Guid id)
        : base(id)
    {
    }

    /// <summary>Owning sequence.</summary>
    public Guid SequenceId { get; private set; }

    /// <summary>Step order within the sequence (ascending; gaps are fine).</summary>
    public int StepOrder { get; private set; }

    /// <summary>Days to wait after the previous step (or after enrolment for the first step); ≥ 0.</summary>
    public int DelayDays { get; private set; }

    /// <summary>Email subject.</summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>Email body.</summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>When the step was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Adds a step to a sequence.</summary>
    /// <param name="sequenceId">Owning sequence id.</param>
    /// <param name="stepOrder">Order within the sequence.</param>
    /// <param name="delayDays">Days after the previous step (≥ 0).</param>
    /// <param name="subject">Subject (required, ≤ 200 chars).</param>
    /// <param name="body">Body (required, ≤ 10000 chars).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The step, or a validation error.</returns>
    public static Result<NurtureStep> Create(Guid sequenceId, int stepOrder, int delayDays, string subject, string body, DateTimeOffset createdAt)
    {
        if (delayDays < 0)
        {
            return Error.Validation("nurture.delay_invalid", "Delay days cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 200)
        {
            return Error.Validation("nurture.subject_invalid", "A subject (≤ 200 chars) is required.");
        }

        if (string.IsNullOrWhiteSpace(body) || body.Length > 10000)
        {
            return Error.Validation("nurture.body_invalid", "A body (≤ 10000 chars) is required.");
        }

        return new NurtureStep(Guid.NewGuid())
        {
            SequenceId = sequenceId,
            StepOrder = stepOrder,
            DelayDays = delayDays,
            Subject = subject.Trim(),
            Body = body.Trim(),
            CreatedAt = createdAt,
        };
    }
}

/// <summary>A recipient's enrolment in a sequence, tracking the next step due and when to send it.</summary>
public sealed class NurtureEnrollment : Entity<Guid>
{
    private NurtureEnrollment(Guid id)
        : base(id)
    {
    }

    /// <summary>The sequence this recipient is enrolled in.</summary>
    public Guid SequenceId { get; private set; }

    /// <summary>Recipient email.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Recipient display name (optional).</summary>
    public string? Name { get; private set; }

    /// <summary>Enrolment status.</summary>
    public EnrollmentStatus Status { get; private set; }

    /// <summary>The <see cref="NurtureStep.StepOrder"/> of the next step to send.</summary>
    public int NextStepOrder { get; private set; }

    /// <summary>When the next step becomes due (UTC).</summary>
    public DateTimeOffset NextSendAt { get; private set; }

    /// <summary>When the recipient was enrolled (UTC).</summary>
    public DateTimeOffset EnrolledAt { get; private set; }

    /// <summary>When the enrolment last changed (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Enrols a recipient, scheduling the first step.</summary>
    /// <param name="sequenceId">The sequence.</param>
    /// <param name="email">Recipient email (required).</param>
    /// <param name="name">Recipient name (optional).</param>
    /// <param name="firstStepOrder">The first step's order.</param>
    /// <param name="firstDelayDays">The first step's delay (days after enrolment).</param>
    /// <param name="now">Enrolment timestamp (UTC).</param>
    /// <returns>The enrolment, or a validation error.</returns>
    public static Result<NurtureEnrollment> Enroll(Guid sequenceId, string email, string? name, int firstStepOrder, int firstDelayDays, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            return Error.Validation("nurture.email_invalid", "A valid recipient email is required.");
        }

        return new NurtureEnrollment(Guid.NewGuid())
        {
            SequenceId = sequenceId,
            Email = email.Trim(),
            Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
            Status = EnrollmentStatus.Active,
            NextStepOrder = firstStepOrder,
            NextSendAt = now.AddDays(Math.Max(0, firstDelayDays)),
            EnrolledAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>Advances to the next step, scheduling its send.</summary>
    /// <param name="nextStepOrder">The next step's order.</param>
    /// <param name="delayDays">The next step's delay (days from now).</param>
    /// <param name="now">Reference time (UTC).</param>
    public void AdvanceTo(int nextStepOrder, int delayDays, DateTimeOffset now)
    {
        NextStepOrder = nextStepOrder;
        NextSendAt = now.AddDays(Math.Max(0, delayDays));
        UpdatedAt = now;
    }

    /// <summary>Marks the enrolment complete (all steps sent).</summary>
    /// <param name="now">Reference time (UTC).</param>
    public void Complete(DateTimeOffset now)
    {
        Status = EnrollmentStatus.Completed;
        UpdatedAt = now;
    }

    /// <summary>Stops the enrolment before completion.</summary>
    /// <param name="now">Reference time (UTC).</param>
    public void Stop(DateTimeOffset now)
    {
        Status = EnrollmentStatus.Stopped;
        UpdatedAt = now;
    }
}
