using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class InterviewKitTests
{
    private static readonly Guid KitId = Guid.NewGuid();

    [Fact]
    public async Task Adding_a_question_appends_at_the_next_order()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetInterviewKitAsync(KitId, Arg.Any<CancellationToken>()).Returns(InterviewKit.Create("Backend loop", DateTimeOffset.UnixEpoch).Value!);
        repo.ListKitQuestionsAsync(KitId, Arg.Any<CancellationToken>())
            .Returns(new[] { InterviewKitQuestion.Create(KitId, 1, "Explain async/await", "C#", DateTimeOffset.UnixEpoch).Value! });
        var handler = new AddKitQuestionCommandHandler(repo);

        var result = await handler.HandleAsync(new AddKitQuestionCommand(KitId, "Design a rate limiter", "System Design"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.QuestionOrder.Should().Be(2);
        result.Value!.Skill.Should().Be("System Design");
        repo.Received(1).AddKitQuestion(Arg.Any<InterviewKitQuestion>());
    }

    [Fact]
    public async Task Adding_a_question_to_a_missing_kit_returns_not_found()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetInterviewKitAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((InterviewKit?)null);
        var handler = new AddKitQuestionCommandHandler(repo);

        var result = await handler.HandleAsync(new AddKitQuestionCommand(Guid.NewGuid(), "Q", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("kit.not_found");
    }

    [Fact]
    public async Task Blank_kit_name_is_rejected()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        var handler = new CreateInterviewKitCommandHandler(repo);

        var result = await handler.HandleAsync(new CreateInterviewKitCommand("   "), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("kit.name_invalid");
    }
}
