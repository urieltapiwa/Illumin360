using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;
using ApplicationId = Illumin360.Recruitment.Domain.ApplicationId;

namespace Illumin360.Recruitment.UnitTests;

public class ApplicationMessagesTests
{
    private static RecruitmentApplication AnApplication()
        => RecruitmentApplication.Apply(new RequestId(Guid.NewGuid()), Guid.NewGuid(), "professional", DateTimeOffset.UnixEpoch);

    private static ApplicationMessage Msg(Guid appId, MessageSender sender)
        => ApplicationMessage.Post(appId, sender.ToWire(), sender == MessageSender.Recruiter ? "Rita" : "Cara", "hi", DateTimeOffset.UnixEpoch).Value!;

    [Fact]
    public void Post_validates_sender_and_body()
    {
        var appId = Guid.NewGuid();
        ApplicationMessage.Post(appId, "boss", "Rita", "hi", DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        ApplicationMessage.Post(appId, "recruiter", "Rita", "  ", DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        ApplicationMessage.Post(appId, "talent", "Cara", "Hello there", DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Send_to_missing_application_is_not_found()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>()).Returns((RecruitmentApplication?)null);
        var handler = new SendApplicationMessageCommandHandler(repo);

        var result = await handler.HandleAsync(new SendApplicationMessageCommand(Guid.NewGuid(), "recruiter", "Rita", "hi"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Send_persists_message()
    {
        var app = AnApplication();
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>()).Returns(app);
        var handler = new SendApplicationMessageCommandHandler(repo);

        var result = await handler.HandleAsync(new SendApplicationMessageCommand(app.Id.Value, "recruiter", "Rita", "Can you interview Tuesday?"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Sender.Should().Be("recruiter");
        repo.Received(1).AddApplicationMessage(Arg.Any<ApplicationMessage>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Mark_read_marks_only_counterpart_messages()
    {
        var appId = Guid.NewGuid();
        var fromRecruiter = Msg(appId, MessageSender.Recruiter);
        var fromTalent = Msg(appId, MessageSender.Talent);
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.ListApplicationMessagesTrackedAsync(appId, Arg.Any<CancellationToken>()).Returns(new[] { fromRecruiter, fromTalent });
        var handler = new MarkThreadReadCommandHandler(repo);

        // Talent reads → only the recruiter's message is marked read.
        var result = await handler.HandleAsync(new MarkThreadReadCommand(appId, "talent"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().Be(1);
        fromRecruiter.IsRead.Should().BeTrue();
        fromTalent.IsRead.Should().BeFalse();
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Mark_read_rejects_bad_reader()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        var handler = new MarkThreadReadCommandHandler(repo);

        var result = await handler.HandleAsync(new MarkThreadReadCommand(Guid.NewGuid(), "boss"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("message.sender_invalid");
    }
}
