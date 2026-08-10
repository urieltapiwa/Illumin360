using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.Recruitment.IntegrationEvents;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class JobAlertRunnerTests
{
    [Fact]
    public async Task Publishes_a_digest_only_for_searches_with_matches()
    {
        var withMatches = SavedSearch.Create(Guid.NewGuid(), "Dev", null, "developer", true, DateTimeOffset.UnixEpoch).Value!;
        var noMatches = SavedSearch.Create(Guid.NewGuid(), "Pilot", null, "pilot", true, DateTimeOffset.UnixEpoch).Value!;
        var dev = RecruitmentRequest.Post(Guid.NewGuid(), "Software Developer", "Windhoek", 1).Value!;

        var repo = Substitute.For<IRecruitmentRepository>();
        repo.ListAlertEnabledSavedSearchesAsync(Arg.Any<CancellationToken>()).Returns(new[] { withMatches, noMatches });
        repo.ListAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { dev });

        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var runner = new JobAlertRunner(repo, publisher);

        var published = await runner.RunOnceAsync(DateTimeOffset.UnixEpoch, CancellationToken.None);

        published.Should().Be(1);
        await publisher.Received(1).PublishAsync(
            Arg.Is<JobAlertDigest>(d => d.TalentId == withMatches.TalentId && d.MatchCount == 1),
            Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publishes_nothing_when_no_alert_searches()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.ListAlertEnabledSavedSearchesAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<SavedSearch>());
        var publisher = Substitute.For<IIntegrationEventPublisher>();

        var published = await new JobAlertRunner(repo, publisher).RunOnceAsync(DateTimeOffset.UnixEpoch, CancellationToken.None);

        published.Should().Be(0);
        await publisher.DidNotReceiveWithAnyArgs().PublishAsync<JobAlertDigest>(default!, default);
    }
}
