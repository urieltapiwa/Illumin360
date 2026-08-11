using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;
using ApplicationId = Illumin360.Recruitment.Domain.ApplicationId;

namespace Illumin360.Recruitment.UnitTests;

public class ApplicationSourcesTests
{
    [Fact]
    public void Normalize_lowercases_trims_and_defaults()
    {
        ApplicationSource.Normalize(null).Should().Be("direct");
        ApplicationSource.Normalize("   ").Should().Be("direct");
        ApplicationSource.Normalize("  Referral ").Should().Be("referral");
    }

    [Fact]
    public void Create_normalises_channel()
    {
        var ok = ApplicationSource.Create(Guid.NewGuid(), " Campaign ", DateTimeOffset.UnixEpoch);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.Channel.Should().Be("campaign");

        ApplicationSource.Create(Guid.Empty, "careers", DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Get_source_defaults_to_direct_when_none_recorded()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        var appId = Guid.NewGuid();
        repo.GetApplicationSourceAsync(appId, Arg.Any<CancellationToken>()).Returns((ApplicationSource?)null);
        var handler = new GetApplicationSourceQueryHandler(repo);

        var result = await handler.HandleAsync(new GetApplicationSourceQuery(appId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Channel.Should().Be("direct");
    }

    [Fact]
    public async Task Set_source_creates_when_absent()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        var appId = Guid.NewGuid();
        repo.GetApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>())
            .Returns(RecruitmentApplication.Apply(new RequestId(Guid.NewGuid()), Guid.NewGuid(), "professional", DateTimeOffset.UnixEpoch));
        repo.GetApplicationSourceTrackedAsync(appId, Arg.Any<CancellationToken>()).Returns((ApplicationSource?)null);
        var handler = new SetApplicationSourceCommandHandler(repo);

        var result = await handler.HandleAsync(new SetApplicationSourceCommand(appId, "board"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Channel.Should().Be("board");
        repo.Received(1).AddApplicationSource(Arg.Is<ApplicationSource>(s => s.Channel == "board"));
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_source_missing_application_is_not_found()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>()).Returns((RecruitmentApplication?)null);
        var handler = new SetApplicationSourceCommandHandler(repo);

        var result = await handler.HandleAsync(new SetApplicationSourceCommand(Guid.NewGuid(), "referral"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}
