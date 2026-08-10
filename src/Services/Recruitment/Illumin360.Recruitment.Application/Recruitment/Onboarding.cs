using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A single onboarding task.</summary>
/// <param name="Id">Task id.</param>
/// <param name="Label">Task label.</param>
/// <param name="IsDone">Whether complete.</param>
/// <param name="SortOrder">Presentation order.</param>
public sealed record OnboardingTaskDto(Guid Id, string Label, bool IsDone, int SortOrder)
{
    /// <summary>Projects a domain <see cref="OnboardingTask"/> into the transport DTO.</summary>
    /// <param name="t">The task.</param>
    /// <returns>The transport DTO.</returns>
    public static OnboardingTaskDto FromDomain(OnboardingTask t)
    {
        ArgumentNullException.ThrowIfNull(t);
        return new OnboardingTaskDto(t.Id.Value, t.Label, t.IsDone, t.SortOrder);
    }
}

/// <summary>An onboarding checklist with its tasks and progress.</summary>
/// <param name="Id">Checklist id.</param>
/// <param name="ApplicationId">The application the checklist is for.</param>
/// <param name="RoleTitle">Role title.</param>
/// <param name="Completed">Number of completed tasks.</param>
/// <param name="Total">Total number of tasks.</param>
/// <param name="Tasks">The tasks, in order.</param>
public sealed record OnboardingChecklistDto(Guid Id, Guid ApplicationId, string RoleTitle, int Completed, int Total, IReadOnlyList<OnboardingTaskDto> Tasks)
{
    /// <summary>Projects a checklist + its tasks into the transport DTO.</summary>
    /// <param name="c">The checklist.</param>
    /// <param name="tasks">The checklist's tasks.</param>
    /// <returns>The transport DTO.</returns>
    public static OnboardingChecklistDto FromDomain(OnboardingChecklist c, IReadOnlyList<OnboardingTask> tasks)
    {
        ArgumentNullException.ThrowIfNull(c);
        ArgumentNullException.ThrowIfNull(tasks);
        var ordered = tasks.OrderBy(t => t.SortOrder).Select(OnboardingTaskDto.FromDomain).ToList();
        return new OnboardingChecklistDto(c.Id.Value, c.ApplicationId, c.RoleTitle, ordered.Count(t => t.IsDone), ordered.Count, ordered);
    }
}

/// <summary>Starts an onboarding checklist (with default tasks) for a hired application.</summary>
public sealed record StartOnboardingCommand(Guid ApplicationId, string RoleTitle) : ICommand<OnboardingChecklistDto>;

/// <summary>Gets the onboarding checklist for an application.</summary>
public sealed record GetOnboardingQuery(Guid ApplicationId) : IQuery<OnboardingChecklistDto>;

/// <summary>Toggles an onboarding task's completion state.</summary>
public sealed record ToggleOnboardingTaskCommand(Guid TaskId, bool Done) : ICommand<OnboardingTaskDto>;

/// <summary>Adds a custom task to a checklist.</summary>
public sealed record AddOnboardingTaskCommand(Guid ChecklistId, string Label) : ICommand<OnboardingTaskDto>;

/// <summary>Removes a task from a checklist.</summary>
public sealed record RemoveOnboardingTaskCommand(Guid TaskId) : ICommand<bool>;

/// <summary>Handles <see cref="StartOnboardingCommand"/> — creates the checklist and its default tasks.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class StartOnboardingCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<StartOnboardingCommand, OnboardingChecklistDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<OnboardingChecklistDto>> HandleAsync(StartOnboardingCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await _repository.GetChecklistByApplicationAsync(command.ApplicationId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return Error.Conflict("onboarding.exists", "This application already has an onboarding checklist.");
        }

        var creation = OnboardingChecklist.Start(command.ApplicationId, command.RoleTitle, DateTimeOffset.UtcNow);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        var checklist = creation.Value!;
        _repository.AddOnboardingChecklist(checklist);

        var tasks = new List<OnboardingTask>();
        for (var i = 0; i < OnboardingChecklist.DefaultTasks.Count; i++)
        {
            var task = OnboardingTask.Create(checklist.Id, OnboardingChecklist.DefaultTasks[i], i).Value!;
            _repository.AddOnboardingTask(task);
            tasks.Add(task);
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return OnboardingChecklistDto.FromDomain(checklist, tasks);
    }
}

/// <summary>Handles <see cref="GetOnboardingQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetOnboardingQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetOnboardingQuery, OnboardingChecklistDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<OnboardingChecklistDto>> HandleAsync(GetOnboardingQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var checklist = await _repository.GetChecklistByApplicationAsync(query.ApplicationId, cancellationToken).ConfigureAwait(false);
        if (checklist is null)
        {
            return Error.NotFound("onboarding.not_found", "No onboarding checklist for this application.");
        }

        var tasks = await _repository.ListTasksForChecklistAsync(checklist.Id, cancellationToken).ConfigureAwait(false);
        return OnboardingChecklistDto.FromDomain(checklist, tasks);
    }
}

/// <summary>Handles <see cref="ToggleOnboardingTaskCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class ToggleOnboardingTaskCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<ToggleOnboardingTaskCommand, OnboardingTaskDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<OnboardingTaskDto>> HandleAsync(ToggleOnboardingTaskCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var task = await _repository.GetOnboardingTaskAsync(new OnboardingTaskId(command.TaskId), cancellationToken).ConfigureAwait(false);
        if (task is null)
        {
            return Error.NotFound("onboarding.task_not_found", "No matching onboarding task.");
        }

        task.SetDone(command.Done, DateTimeOffset.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return OnboardingTaskDto.FromDomain(task);
    }
}

/// <summary>Handles <see cref="AddOnboardingTaskCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class AddOnboardingTaskCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<AddOnboardingTaskCommand, OnboardingTaskDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<OnboardingTaskDto>> HandleAsync(AddOnboardingTaskCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var checklistId = new OnboardingChecklistId(command.ChecklistId);
        var checklist = await _repository.GetChecklistAsync(checklistId, cancellationToken).ConfigureAwait(false);
        if (checklist is null)
        {
            return Error.NotFound("onboarding.not_found", "No matching onboarding checklist.");
        }

        var existing = await _repository.ListTasksForChecklistAsync(checklistId, cancellationToken).ConfigureAwait(false);
        var nextOrder = existing.Count == 0 ? 0 : existing.Max(t => t.SortOrder) + 1;

        var creation = OnboardingTask.Create(checklistId, command.Label, nextOrder);
        if (creation.IsFailure)
        {
            return creation.Error!;
        }

        _repository.AddOnboardingTask(creation.Value!);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return OnboardingTaskDto.FromDomain(creation.Value!);
    }
}

/// <summary>Handles <see cref="RemoveOnboardingTaskCommand"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class RemoveOnboardingTaskCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<RemoveOnboardingTaskCommand, bool>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(RemoveOnboardingTaskCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var task = await _repository.GetOnboardingTaskAsync(new OnboardingTaskId(command.TaskId), cancellationToken).ConfigureAwait(false);
        if (task is null)
        {
            return Error.NotFound("onboarding.task_not_found", "No matching onboarding task.");
        }

        _repository.RemoveOnboardingTask(task);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
