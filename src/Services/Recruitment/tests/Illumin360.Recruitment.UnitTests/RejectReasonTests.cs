using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;
using ApplicationId = Illumin360.Recruitment.Domain.ApplicationId;

namespace Illumin360.Recruitment.UnitTests;

public class RejectReasonTests
{
    private static RecruitmentApplication AnApplication()
        => RecruitmentApplication.Apply(new RequestId(Guid.NewGuid()), Guid.NewGuid(), "professional", DateTimeOffset.UnixEpoch);

    [Fact]
    public void Rejection_validates_reason()
    {
        var id = Guid.NewGuid();
        ApplicationRejection.Create(id, "  ", null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        ApplicationRejection.Create(id, new string('x', 1001), null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        ApplicationRejection.Create(id, "Not enough experience", "Rita", DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Reject_without_reason_records_nothing()
    {
        var app = AnApplication();
        var repo = Substitute.For<IRecruitmentRepository>();
        var pub = Substitute.For<IIntegrationEventPublisher>();
        repo.GetApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>()).Returns(app);
        var handler = new RejectApplicationCommandHandler(repo, pub);

        var result = await handler.HandleAsync(new RejectApplicationCommand(app.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RejectReason.Should().BeNull();
        repo.DidNotReceive().AddApplicationRejection(Arg.Any<ApplicationRejection>());
    }

    [Fact]
    public async Task Reject_with_reason_records_and_returns_it()
    {
        var app = AnApplication();
        var repo = Substitute.For<IRecruitmentRepository>();
        var pub = Substitute.For<IIntegrationEventPublisher>();
        repo.GetApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>()).Returns(app);
        var handler = new RejectApplicationCommandHandler(repo, pub);

        var result = await handler.HandleAsync(new RejectApplicationCommand(app.Id.Value, "Role filled internally", "Rita"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("rejected");
        result.Value!.RejectReason.Should().Be("Role filled internally");
        repo.Received(1).AddApplicationRejection(Arg.Is<ApplicationRejection>(r => r.Reason == "Role filled internally" && r.RejectedBy == "Rita"));
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_missing_application_is_not_found()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        var pub = Substitute.For<IIntegrationEventPublisher>();
        repo.GetApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>()).Returns((RecruitmentApplication?)null);
        var handler = new RejectApplicationCommandHandler(repo, pub);

        var result = await handler.HandleAsync(new RejectApplicationCommand(Guid.NewGuid(), "x"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}
