using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class ApplicationFormsTests
{
    [Fact]
    public void Question_validates_label_and_select_options()
    {
        ApplicationFormQuestion.Create(Guid.NewGuid(), "  ", "text", null, false, 0, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();

        // Select needs at least two options.
        ApplicationFormQuestion.Create(Guid.NewGuid(), "Pick one", "select", ["only"], false, 0, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();

        var ok = ApplicationFormQuestion.Create(Guid.NewGuid(), " Years of Go? ", "number", null, true, 3, DateTimeOffset.UnixEpoch);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.Label.Should().Be("Years of Go?");
        ok.Value!.Kind.Should().Be(QuestionKind.Number);
        ok.Value!.Required.Should().BeTrue();

        var select = ApplicationFormQuestion.Create(Guid.NewGuid(), "Notice period", "select", ["Immediate", " 1 month ", "3 months"], false, 0, DateTimeOffset.UnixEpoch);
        select.IsSuccess.Should().BeTrue();
        select.Value!.Options.Should().BeEquivalentTo("Immediate", "1 month", "3 months");
        // Non-select questions carry no options.
        ok.Value!.Options.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_question_appends_after_existing()
    {
        var requestId = Guid.NewGuid();
        var existing = ApplicationFormQuestion.Create(requestId, "First", "text", null, false, 5, DateTimeOffset.UnixEpoch).Value!;
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.ListFormQuestionsAsync(requestId, Arg.Any<CancellationToken>()).Returns(new[] { existing });
        var handler = new AddFormQuestionCommandHandler(repo);

        var result = await handler.HandleAsync(new AddFormQuestionCommand(requestId, "Second", "text", null, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SortOrder.Should().Be(6); // max(5) + 1
        repo.Received(1).AddFormQuestion(Arg.Any<ApplicationFormQuestion>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_snapshots_labels_replaces_prior_and_skips_unknown_or_blank()
    {
        var appId = Guid.NewGuid();
        var q1 = ApplicationFormQuestion.Create(Guid.NewGuid(), "Why this role?", "textarea", null, true, 0, DateTimeOffset.UnixEpoch).Value!;
        var prior = ApplicationAnswer.Create(appId, Guid.NewGuid(), "old", "old value", DateTimeOffset.UnixEpoch).Value!;

        var repo = Substitute.For<IRecruitmentRepository>();
        repo.ListApplicationAnswersTrackedAsync(appId, Arg.Any<CancellationToken>()).Returns(new[] { prior });
        repo.GetFormQuestionAsync(q1.Id, Arg.Any<CancellationToken>()).Returns(q1);
        repo.GetFormQuestionAsync(Arg.Is<Guid>(g => g != q1.Id), Arg.Any<CancellationToken>()).Returns((ApplicationFormQuestion?)null);
        var handler = new SubmitApplicationAnswersCommandHandler(repo);

        var inputs = new List<AnswerInput>
        {
            new(q1.Id, "Because I love Go"),      // valid
            new(Guid.NewGuid(), "orphan answer"),  // unknown question → skipped
            new(q1.Id, "   "),                     // blank → skipped
        };
        var result = await handler.HandleAsync(new SubmitApplicationAnswersCommand(appId, inputs), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1); // only the valid one persisted
        repo.Received(1).RemoveApplicationAnswer(prior); // prior answers cleared
        repo.Received(1).AddApplicationAnswer(Arg.Is<ApplicationAnswer>(a => a.QuestionLabel == "Why this role?" && a.Value == "Because I love Go"));
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Remove_missing_question_is_not_found()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetFormQuestionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ApplicationFormQuestion?)null);
        var handler = new RemoveFormQuestionCommandHandler(repo);

        var result = await handler.HandleAsync(new RemoveFormQuestionCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}
