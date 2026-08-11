using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.Recruitment.IntegrationEvents;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A nurture sequence summary.</summary>
/// <param name="Id">Sequence id.</param>
/// <param name="Name">Sequence name.</param>
/// <param name="Status">Status (Active/Archived).</param>
/// <param name="StepCount">Number of steps.</param>
/// <param name="ActiveEnrollments">Recipients still progressing.</param>
/// <param name="CreatedAt">Creation timestamp.</param>
public sealed record NurtureSequenceDto(Guid Id, string Name, string Status, int StepCount, int ActiveEnrollments, DateTimeOffset CreatedAt);

/// <summary>A nurture step.</summary>
/// <param name="Id">Step id.</param>
/// <param name="StepOrder">Order within the sequence.</param>
/// <param name="DelayDays">Days after the previous step.</param>
/// <param name="Subject">Email subject.</param>
/// <param name="Body">Email body.</param>
public sealed record NurtureStepDto(Guid Id, int StepOrder, int DelayDays, string Subject, string Body);

/// <summary>A nurture enrolment.</summary>
/// <param name="Id">Enrolment id.</param>
/// <param name="Email">Recipient email.</param>
/// <param name="Name">Recipient name.</param>
/// <param name="Status">Status (Active/Completed/Stopped).</param>
/// <param name="NextStepOrder">Next step due.</param>
/// <param name="NextSendAt">When the next step sends.</param>
public sealed record NurtureEnrollmentDto(Guid Id, string Email, string? Name, string Status, int NextStepOrder, DateTimeOffset NextSendAt);

/// <summary>A sequence with its steps and enrolments.</summary>
/// <param name="Sequence">The sequence summary.</param>
/// <param name="Steps">Its steps, in order.</param>
/// <param name="Enrollments">Its enrolments.</param>
public sealed record NurtureSequenceDetailDto(NurtureSequenceDto Sequence, IReadOnlyList<NurtureStepDto> Steps, IReadOnlyList<NurtureEnrollmentDto> Enrollments);

/// <summary>Creates a nurture sequence.</summary>
/// <param name="Name">Sequence name.</param>
public sealed record CreateNurtureSequenceCommand(string Name) : ICommand<NurtureSequenceDto>;

/// <summary>Adds a step to a sequence.</summary>
/// <param name="SequenceId">The sequence.</param>
/// <param name="DelayDays">Days after the previous step.</param>
/// <param name="Subject">Email subject.</param>
/// <param name="Body">Email body.</param>
public sealed record AddNurtureStepCommand(Guid SequenceId, int DelayDays, string Subject, string Body) : ICommand<NurtureStepDto>;

/// <summary>Enrols a recipient into a sequence.</summary>
/// <param name="SequenceId">The sequence.</param>
/// <param name="Email">Recipient email.</param>
/// <param name="Name">Recipient name (optional).</param>
public sealed record EnrollRecipientCommand(Guid SequenceId, string Email, string? Name) : ICommand<NurtureEnrollmentDto>;

/// <summary>Stops an active enrolment.</summary>
/// <param name="EnrollmentId">The enrolment.</param>
public sealed record StopEnrollmentCommand(Guid EnrollmentId) : ICommand<NurtureEnrollmentDto>;

/// <summary>Lists all nurture sequences.</summary>
public sealed record ListNurtureSequencesQuery : IQuery<IReadOnlyList<NurtureSequenceDto>>;

/// <summary>Gets a sequence with its steps and enrolments.</summary>
/// <param name="SequenceId">The sequence.</param>
public sealed record GetNurtureSequenceQuery(Guid SequenceId) : IQuery<NurtureSequenceDetailDto>;

/// <summary>Handles <see cref="CreateNurtureSequenceCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class CreateNurtureSequenceCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<CreateNurtureSequenceCommand, NurtureSequenceDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<NurtureSequenceDto>> HandleAsync(CreateNurtureSequenceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = NurtureSequence.Create(command.Name, DateTimeOffset.UtcNow);
        if (created.IsFailure)
        {
            return created.Error!;
        }

        _repository.AddNurtureSequence(created.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new NurtureSequenceDto(created.Value!.Id, created.Value!.Name, created.Value!.Status.ToString(), 0, 0, created.Value!.CreatedAt);
    }
}

/// <summary>Handles <see cref="AddNurtureStepCommand"/> — appends a step at the next order.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class AddNurtureStepCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<AddNurtureStepCommand, NurtureStepDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<NurtureStepDto>> HandleAsync(AddNurtureStepCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sequence = await _repository.GetNurtureSequenceAsync(command.SequenceId, cancellationToken).ConfigureAwait(false);
        if (sequence is null)
        {
            return Error.NotFound("nurture.sequence_not_found", "Sequence not found.");
        }

        var existing = await _repository.ListNurtureStepsAsync(command.SequenceId, cancellationToken).ConfigureAwait(false);
        var nextOrder = existing.Count == 0 ? 1 : existing.Max(s => s.StepOrder) + 1;

        var step = NurtureStep.Create(command.SequenceId, nextOrder, command.DelayDays, command.Subject, command.Body, DateTimeOffset.UtcNow);
        if (step.IsFailure)
        {
            return step.Error!;
        }

        _repository.AddNurtureStep(step.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new NurtureStepDto(step.Value!.Id, step.Value!.StepOrder, step.Value!.DelayDays, step.Value!.Subject, step.Value!.Body);
    }
}

/// <summary>Handles <see cref="EnrollRecipientCommand"/> — schedules the first step.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class EnrollRecipientCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<EnrollRecipientCommand, NurtureEnrollmentDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<NurtureEnrollmentDto>> HandleAsync(EnrollRecipientCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sequence = await _repository.GetNurtureSequenceAsync(command.SequenceId, cancellationToken).ConfigureAwait(false);
        if (sequence is null)
        {
            return Error.NotFound("nurture.sequence_not_found", "Sequence not found.");
        }

        var steps = await _repository.ListNurtureStepsAsync(command.SequenceId, cancellationToken).ConfigureAwait(false);
        if (steps.Count == 0)
        {
            return Error.Validation("nurture.no_steps", "Add at least one step before enrolling recipients.");
        }

        if (await _repository.IsEnrolledAsync(command.SequenceId, command.Email?.Trim() ?? string.Empty, cancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict("nurture.already_enrolled", "That recipient is already enrolled in this sequence.");
        }

        var first = steps.OrderBy(s => s.StepOrder).First();
        var enrollment = NurtureEnrollment.Enroll(command.SequenceId, command.Email ?? string.Empty, command.Name, first.StepOrder, first.DelayDays, DateTimeOffset.UtcNow);
        if (enrollment.IsFailure)
        {
            return enrollment.Error!;
        }

        _repository.AddNurtureEnrollment(enrollment.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var e = enrollment.Value!;
        return new NurtureEnrollmentDto(e.Id, e.Email, e.Name, e.Status.ToString(), e.NextStepOrder, e.NextSendAt);
    }
}

/// <summary>Handles <see cref="StopEnrollmentCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class StopEnrollmentCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<StopEnrollmentCommand, NurtureEnrollmentDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<NurtureEnrollmentDto>> HandleAsync(StopEnrollmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var enrollment = await _repository.GetNurtureEnrollmentAsync(command.EnrollmentId, cancellationToken).ConfigureAwait(false);
        if (enrollment is null)
        {
            return Error.NotFound("nurture.enrollment_not_found", "Enrolment not found.");
        }

        if (enrollment.Status != EnrollmentStatus.Active)
        {
            return Error.Conflict("nurture.enrollment_not_active", "This enrolment is not active.");
        }

        enrollment.Stop(DateTimeOffset.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new NurtureEnrollmentDto(enrollment.Id, enrollment.Email, enrollment.Name, enrollment.Status.ToString(), enrollment.NextStepOrder, enrollment.NextSendAt);
    }
}

/// <summary>Handles <see cref="ListNurtureSequencesQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class ListNurtureSequencesQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<ListNurtureSequencesQuery, IReadOnlyList<NurtureSequenceDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<NurtureSequenceDto>>> HandleAsync(ListNurtureSequencesQuery query, CancellationToken cancellationToken)
    {
        var sequences = await _repository.ListNurtureSequencesAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<NurtureSequenceDto>(sequences.Count);
        foreach (var s in sequences)
        {
            var steps = await _repository.ListNurtureStepsAsync(s.Id, cancellationToken).ConfigureAwait(false);
            var active = await _repository.CountActiveEnrollmentsAsync(s.Id, cancellationToken).ConfigureAwait(false);
            result.Add(new NurtureSequenceDto(s.Id, s.Name, s.Status.ToString(), steps.Count, active, s.CreatedAt));
        }

        return result;
    }
}

/// <summary>Handles <see cref="GetNurtureSequenceQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetNurtureSequenceQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetNurtureSequenceQuery, NurtureSequenceDetailDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<NurtureSequenceDetailDto>> HandleAsync(GetNurtureSequenceQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sequence = await _repository.GetNurtureSequenceAsync(query.SequenceId, cancellationToken).ConfigureAwait(false);
        if (sequence is null)
        {
            return Error.NotFound("nurture.sequence_not_found", "Sequence not found.");
        }

        var steps = await _repository.ListNurtureStepsAsync(query.SequenceId, cancellationToken).ConfigureAwait(false);
        var enrollments = await _repository.ListEnrollmentsForSequenceAsync(query.SequenceId, cancellationToken).ConfigureAwait(false);

        var summary = new NurtureSequenceDto(sequence.Id, sequence.Name, sequence.Status.ToString(), steps.Count, enrollments.Count(e => e.Status == EnrollmentStatus.Active), sequence.CreatedAt);
        return new NurtureSequenceDetailDto(
            summary,
            steps.OrderBy(s => s.StepOrder).Select(s => new NurtureStepDto(s.Id, s.StepOrder, s.DelayDays, s.Subject, s.Body)).ToList(),
            enrollments.Select(e => new NurtureEnrollmentDto(e.Id, e.Email, e.Name, e.Status.ToString(), e.NextStepOrder, e.NextSendAt)).ToList());
    }
}

/// <summary>
/// Advances due nurture enrolments: for each active enrolment whose next step is due, sends that step's
/// email (via the shared campaign-email event → Notifications worker) and schedules the next step, or
/// completes the enrolment when the last step has been sent. Deterministic; safe to run on a timer.
/// </summary>
/// <param name="repository">The recruitment repository.</param>
/// <param name="eventPublisher">The integration-event publisher (transactional outbox).</param>
public sealed class NurtureRunner(IRecruitmentRepository repository, IIntegrationEventPublisher eventPublisher)
{
    private readonly IRecruitmentRepository _repository = repository;
    private readonly IIntegrationEventPublisher _eventPublisher = eventPublisher;

    /// <summary>Runs one pass: sends every due step and advances the enrolments.</summary>
    /// <param name="now">The reference time (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of step emails sent.</returns>
    public async Task<int> RunOnceAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var due = await _repository.ListDueEnrollmentsAsync(now, cancellationToken).ConfigureAwait(false);
        if (due.Count == 0)
        {
            return 0;
        }

        var stepsBySequence = new Dictionary<Guid, List<NurtureStep>>();
        var sent = 0;

        foreach (var enrollment in due)
        {
            if (!stepsBySequence.TryGetValue(enrollment.SequenceId, out var steps))
            {
                steps = (await _repository.ListNurtureStepsAsync(enrollment.SequenceId, cancellationToken).ConfigureAwait(false))
                    .OrderBy(s => s.StepOrder).ToList();
                stepsBySequence[enrollment.SequenceId] = steps;
            }

            var current = steps.FirstOrDefault(s => s.StepOrder == enrollment.NextStepOrder);
            if (current is null)
            {
                enrollment.Complete(now);
                continue;
            }

            await _eventPublisher.PublishAsync(
                new CampaignEmailRequested(enrollment.SequenceId, enrollment.Email, current.Subject, current.Body, now),
                cancellationToken).ConfigureAwait(false);
            sent++;

            var next = steps.FirstOrDefault(s => s.StepOrder > current.StepOrder);
            if (next is null)
            {
                enrollment.Complete(now);
            }
            else
            {
                enrollment.AdvanceTo(next.StepOrder, next.DelayDays, now);
            }
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return sent;
    }
}
