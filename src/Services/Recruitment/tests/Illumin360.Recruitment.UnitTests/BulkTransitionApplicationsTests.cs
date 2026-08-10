using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;
using ApplicationId = Illumin360.Recruitment.Domain.ApplicationId;
using IntegrationEvents = Illumin360.Recruitment.IntegrationEvents;

namespace Illumin360.Recruitment.UnitTests;

public class BulkTransitionApplicationsTests
{
    private static RecruitmentApplication AnApplication()
        => RecruitmentApplication.Apply(new RequestId(Guid.NewGuid()), Guid.NewGuid(), "professional", DateTimeOffset.UnixEpoch);

    private static (IRecruitmentRepository Repo, IIntegrationEventPublisher Pub) Deps()
        => (Substitute.For<IRecruitmentRepository>(), Substitute.For<IIntegrationEventPublisher>());

    [Fact]
    public async Task Empty_batch_is_validation_error()
    {
        var (repo, pub) = Deps();
        var handler = new BulkTransitionApplicationsCommandHandler(repo, pub);

        var result = await handler.HandleAsync(new BulkTransitionApplicationsCommand([], ApplicationBulkAction.Advance), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("bulk.empty");
    }

    [Fact]
    public async Task Advances_found_applications_and_reports_missing()
    {
        var app = AnApplication();
        var (repo, pub) = Deps();
        var missing = Guid.NewGuid();
        repo.GetApplicationAsync(Arg.Is<ApplicationId>(a => a.Value == app.Id.Value), Arg.Any<CancellationToken>()).Returns(app);
        repo.GetApplicationAsync(Arg.Is<ApplicationId>(a => a.Value == missing), Arg.Any<CancellationToken>()).Returns((RecruitmentApplication?)null);
        var handler = new BulkTransitionApplicationsCommandHandler(repo, pub);

        var result = await handler.HandleAsync(new BulkTransitionApplicationsCommand([app.Id.Value, missing], ApplicationBulkAction.Advance), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Requested.Should().Be(2);
        result.Value!.Succeeded.Should().Be(1);
        result.Value!.Failed.Should().Be(1);
        result.Value!.Items.Should().Contain(i => i.ApplicationId == missing && !i.Ok && i.Error == "application.not_found");
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await pub.Received(1).PublishAsync(Arg.Any<IntegrationEvents.ApplicationStatusChanged>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task All_missing_does_not_save()
    {
        var (repo, pub) = Deps();
        repo.GetApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>()).Returns((RecruitmentApplication?)null);
        var handler = new BulkTransitionApplicationsCommandHandler(repo, pub);

        var result = await handler.HandleAsync(new BulkTransitionApplicationsCommand([Guid.NewGuid(), Guid.NewGuid()], ApplicationBulkAction.Reject), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Succeeded.Should().Be(0);
        await repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deduplicates_ids()
    {
        var app = AnApplication();
        var (repo, pub) = Deps();
        repo.GetApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>()).Returns(app);
        var handler = new BulkTransitionApplicationsCommandHandler(repo, pub);

        var result = await handler.HandleAsync(new BulkTransitionApplicationsCommand([app.Id.Value, app.Id.Value], ApplicationBulkAction.Advance), CancellationToken.None);

        result.Value!.Requested.Should().Be(1); // duplicate collapsed
    }
}
