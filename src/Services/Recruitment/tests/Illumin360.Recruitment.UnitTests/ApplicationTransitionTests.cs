using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;
using ApplicationId = Illumin360.Recruitment.Domain.ApplicationId;

namespace Illumin360.Recruitment.UnitTests;

public class ApplicationTransitionTests
{
    private static RecruitmentApplication Applied() =>
        RecruitmentApplication.Apply(RequestId.New(), Guid.NewGuid(), "professional", DateTimeOffset.UnixEpoch);

    [Fact]
    public void Advance_walks_through_stages_to_hired_then_conflicts()
    {
        var app = Applied();

        app.Advance(DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
        app.Status.Should().Be("reviewed");
        app.Advance(DateTimeOffset.UnixEpoch);
        app.Status.Should().Be("shortlisted");
        app.Advance(DateTimeOffset.UnixEpoch);
        app.Status.Should().Be("hired");
        app.IsHire.Should().BeTrue();
        app.DecidedAt.Should().NotBeNull();

        var afterHired = app.Advance(DateTimeOffset.UnixEpoch);
        afterHired.IsFailure.Should().BeTrue();
        afterHired.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void Reject_is_terminal()
    {
        var app = Applied();

        app.Reject(DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
        app.Status.Should().Be("rejected");
        app.DecidedAt.Should().NotBeNull();
        app.Advance(DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        app.Reject(DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AdvanceHandler_unknown_id_returns_not_found()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>()).Returns((RecruitmentApplication?)null);
        var handler = new AdvanceApplicationCommandHandler(repo);

        var result = await handler.HandleAsync(new AdvanceApplicationCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task AdvanceHandler_persists_and_returns_updated_status()
    {
        var app = Applied();
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>()).Returns(app);
        var handler = new AdvanceApplicationCommandHandler(repo);

        var result = await handler.HandleAsync(new AdvanceApplicationCommand(app.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("reviewed");
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
