using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class OnboardingTests
{
    [Fact]
    public void Checklist_start_requires_application_and_role()
    {
        OnboardingChecklist.Start(Guid.Empty, "Dev", DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        OnboardingChecklist.Start(Guid.NewGuid(), " ", DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        OnboardingChecklist.Start(Guid.NewGuid(), "Dev", DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Task_set_done_tracks_completion_timestamp()
    {
        var checklist = OnboardingChecklist.Start(Guid.NewGuid(), "Dev", DateTimeOffset.UnixEpoch).Value!;
        var task = OnboardingTask.Create(checklist.Id, "Sign contract", 0).Value!;

        task.SetDone(true, DateTimeOffset.UnixEpoch);
        task.IsDone.Should().BeTrue();
        task.CompletedAt.Should().NotBeNull();

        task.SetDone(false, DateTimeOffset.UnixEpoch);
        task.IsDone.Should().BeFalse();
        task.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Start_creates_checklist_with_default_tasks()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetChecklistByApplicationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((OnboardingChecklist?)null);
        var handler = new StartOnboardingCommandHandler(repo);

        var result = await handler.HandleAsync(new StartOnboardingCommand(Guid.NewGuid(), "Software Developer"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(OnboardingChecklist.DefaultTasks.Count);
        result.Value!.Completed.Should().Be(0);
        result.Value!.Tasks.Should().HaveCount(OnboardingChecklist.DefaultTasks.Count);
        repo.Received(OnboardingChecklist.DefaultTasks.Count).AddOnboardingTask(Arg.Any<OnboardingTask>());
    }

    [Fact]
    public async Task Start_conflicts_when_checklist_already_exists()
    {
        var existing = OnboardingChecklist.Start(Guid.NewGuid(), "Dev", DateTimeOffset.UnixEpoch).Value!;
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetChecklistByApplicationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(existing);
        var handler = new StartOnboardingCommandHandler(repo);

        var result = await handler.HandleAsync(new StartOnboardingCommand(Guid.NewGuid(), "Dev"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        result.Error!.Code.Should().Be("onboarding.exists");
    }

    [Fact]
    public async Task Toggle_marks_task_done()
    {
        var checklist = OnboardingChecklist.Start(Guid.NewGuid(), "Dev", DateTimeOffset.UnixEpoch).Value!;
        var task = OnboardingTask.Create(checklist.Id, "Sign contract", 0).Value!;
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetOnboardingTaskAsync(Arg.Any<OnboardingTaskId>(), Arg.Any<CancellationToken>()).Returns(task);
        var handler = new ToggleOnboardingTaskCommandHandler(repo);

        var result = await handler.HandleAsync(new ToggleOnboardingTaskCommand(task.Id.Value, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsDone.Should().BeTrue();
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_returns_progress()
    {
        var checklist = OnboardingChecklist.Start(Guid.NewGuid(), "Dev", DateTimeOffset.UnixEpoch).Value!;
        var t1 = OnboardingTask.Create(checklist.Id, "A", 0).Value!;
        var t2 = OnboardingTask.Create(checklist.Id, "B", 1).Value!;
        t1.SetDone(true, DateTimeOffset.UnixEpoch);
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetChecklistByApplicationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(checklist);
        repo.ListTasksForChecklistAsync(Arg.Any<OnboardingChecklistId>(), Arg.Any<CancellationToken>()).Returns(new[] { t1, t2 });
        var handler = new GetOnboardingQueryHandler(repo);

        var result = await handler.HandleAsync(new GetOnboardingQuery(checklist.ApplicationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Completed.Should().Be(1);
        result.Value!.Total.Should().Be(2);
    }
}
