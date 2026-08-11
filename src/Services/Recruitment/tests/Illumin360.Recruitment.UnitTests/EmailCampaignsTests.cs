using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;
using IntegrationEvents = Illumin360.Recruitment.IntegrationEvents;

namespace Illumin360.Recruitment.UnitTests;

public class EmailCampaignsTests
{
    private static EmailCampaign ACampaign()
        => EmailCampaign.Create("Q4 outreach", "We're hiring", "Come work with us.", DateTimeOffset.UnixEpoch).Value!;

    [Fact]
    public void Create_validates_required_fields()
    {
        EmailCampaign.Create("", "s", "b", DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        EmailCampaign.Create("n", "", "b", DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        EmailCampaign.Create("n", "s", "  ", DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        EmailCampaign.Create("n", "s", "b", DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Recipient_validates_email()
    {
        CampaignRecipient.Create(Guid.NewGuid(), "nope").IsFailure.Should().BeTrue();
        var ok = CampaignRecipient.Create(Guid.NewGuid(), "  Cara@Acme.NA ");
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.Email.Should().Be("cara@acme.na");
    }

    [Fact]
    public void MarkSent_requires_recipients_and_is_one_shot()
    {
        var c = ACampaign();
        c.MarkSent(0, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        c.MarkSent(3, DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
        c.Status.Should().Be(CampaignStatus.Sent);
        c.RecipientCount.Should().Be(3);
        c.MarkSent(5, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue(); // already sent
    }

    [Fact]
    public async Task Add_recipient_blocked_after_send()
    {
        var campaign = ACampaign();
        campaign.MarkSent(1, DateTimeOffset.UnixEpoch);
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetCampaignAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(campaign);
        var handler = new AddCampaignRecipientCommandHandler(repo);

        var result = await handler.HandleAsync(new AddCampaignRecipientCommand(campaign.Id, "cara@acme.na"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Send_publishes_one_event_per_recipient_and_marks_sent()
    {
        var campaign = ACampaign();
        var repo = Substitute.For<IRecruitmentRepository>();
        var pub = Substitute.For<IIntegrationEventPublisher>();
        repo.GetCampaignAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(campaign);
        repo.ListCampaignRecipientsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new[]
        {
            CampaignRecipient.Create(campaign.Id, "a@acme.na").Value!,
            CampaignRecipient.Create(campaign.Id, "b@acme.na").Value!,
        });
        var handler = new SendCampaignCommandHandler(repo, pub);

        var result = await handler.HandleAsync(new SendCampaignCommand(campaign.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("sent");
        result.Value!.RecipientCount.Should().Be(2);
        await pub.Received(2).PublishAsync(Arg.Any<IntegrationEvents.CampaignEmailRequested>(), Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_with_no_recipients_fails()
    {
        var campaign = ACampaign();
        var repo = Substitute.For<IRecruitmentRepository>();
        var pub = Substitute.For<IIntegrationEventPublisher>();
        repo.GetCampaignAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(campaign);
        repo.ListCampaignRecipientsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        var handler = new SendCampaignCommandHandler(repo, pub);

        var result = await handler.HandleAsync(new SendCampaignCommand(campaign.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("campaign.no_recipients");
        await pub.DidNotReceive().PublishAsync(Arg.Any<IntegrationEvents.CampaignEmailRequested>(), Arg.Any<CancellationToken>());
    }
}
