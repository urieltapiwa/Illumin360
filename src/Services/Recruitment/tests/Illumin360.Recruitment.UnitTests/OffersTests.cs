using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class OffersTests
{
    private static readonly DateOnly Start = new(2026, 9, 1);

    private static Offer DraftOffer()
        => Offer.Draft(Guid.NewGuid(), "Software Developer", 650000m, "nad", Start, "Great team", DateTimeOffset.UnixEpoch).Value!;

    [Fact]
    public void Draft_validates_and_normalizes_currency()
    {
        Offer.Draft(Guid.NewGuid(), "", 1m, null, Start, null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        Offer.Draft(Guid.NewGuid(), "Dev", 0m, null, Start, null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        Offer.Draft(Guid.NewGuid(), "Dev", 1m, null, default, null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();

        var ok = Offer.Draft(Guid.NewGuid(), "Dev", 500m, "usd", Start, null, DateTimeOffset.UnixEpoch);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.Currency.Should().Be("USD");
        ok.Value!.Status.Should().Be(OfferStatus.Draft);
    }

    [Fact]
    public void Lifecycle_draft_send_accept()
    {
        var o = DraftOffer();
        o.Send().IsSuccess.Should().BeTrue();
        o.Status.Should().Be(OfferStatus.Sent);
        o.Accept(DateTimeOffset.UnixEpoch).IsSuccess.Should().BeTrue();
        o.Status.Should().Be(OfferStatus.Accepted);
        o.DecidedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cannot_accept_before_sent()
    {
        var o = DraftOffer();
        var result = o.Accept(DateTimeOffset.UnixEpoch);
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        result.Error!.Code.Should().Be("offer.not_sent");
    }

    [Fact]
    public void Cannot_send_twice()
    {
        var o = DraftOffer();
        o.Send();
        o.Send().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Withdraw_blocked_after_decision()
    {
        var o = DraftOffer();
        o.Send();
        o.Decline(DateTimeOffset.UnixEpoch);
        var result = o.Withdraw(DateTimeOffset.UnixEpoch);
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("offer.already_final");
    }

    [Fact]
    public async Task Create_handler_persists_draft()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        var handler = new CreateOfferCommandHandler(repo);

        var result = await handler.HandleAsync(
            new CreateOfferCommand(Guid.NewGuid(), "Data Engineer", 720000m, "NAD", Start, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("draft");
        result.Value!.StartDate.Should().Be("2026-09-01");
        repo.Received(1).AddOffer(Arg.Any<Offer>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transition_handler_returns_not_found_for_missing_offer()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetOfferAsync(Arg.Any<OfferId>(), Arg.Any<CancellationToken>()).Returns((Offer?)null);
        var handler = new TransitionOfferCommandHandler(repo);

        var result = await handler.HandleAsync(new TransitionOfferCommand(Guid.NewGuid(), OfferAction.Send), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Transition_handler_sends_and_saves()
    {
        var offer = DraftOffer();
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetOfferAsync(Arg.Any<OfferId>(), Arg.Any<CancellationToken>()).Returns(offer);
        var handler = new TransitionOfferCommandHandler(repo);

        var result = await handler.HandleAsync(new TransitionOfferCommand(offer.Id.Value, OfferAction.Send), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("sent");
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transition_handler_surfaces_conflict()
    {
        var offer = DraftOffer(); // still draft → accept should conflict
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetOfferAsync(Arg.Any<OfferId>(), Arg.Any<CancellationToken>()).Returns(offer);
        var handler = new TransitionOfferCommandHandler(repo);

        var result = await handler.HandleAsync(new TransitionOfferCommand(offer.Id.Value, OfferAction.Accept), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        await repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Sign_accepts_and_records_signature()
    {
        var o = DraftOffer();
        o.Send();
        var result = o.Sign("Jane Candidate", DateTimeOffset.UnixEpoch);
        result.IsSuccess.Should().BeTrue();
        o.Status.Should().Be(OfferStatus.Accepted);
        o.SignedByName.Should().Be("Jane Candidate");
        o.SignedAt.Should().NotBeNull();
    }

    [Fact]
    public void Sign_requires_a_name_and_a_sent_offer()
    {
        var o = DraftOffer();
        o.Send();
        o.Sign("  ", DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();

        var draft = DraftOffer(); // not sent
        var r = draft.Sign("Jane", DateTimeOffset.UnixEpoch);
        r.IsFailure.Should().BeTrue();
        r.Error!.Code.Should().Be("offer.not_sent");
    }

    [Fact]
    public async Task Sign_handler_persists_signature()
    {
        var offer = DraftOffer();
        offer.Send();
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetOfferAsync(Arg.Any<OfferId>(), Arg.Any<CancellationToken>()).Returns(offer);
        var handler = new SignOfferCommandHandler(repo);

        var result = await handler.HandleAsync(new SignOfferCommand(offer.Id.Value, "Jane Candidate"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("accepted");
        result.Value!.SignedByName.Should().Be("Jane Candidate");
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Letter_renders_terms_and_signature_state()
    {
        var offer = OfferDto.FromDomain(DraftOffer());
        var unsigned = OfferLetterHtml.Render(offer, "Illumin360");
        unsigned.Should().Contain("Offer of employment");
        unsigned.Should().Contain("Software Developer");
        unsigned.Should().Contain("Awaiting the candidate's electronic signature.");

        var signedOffer = DraftOffer();
        signedOffer.Send();
        signedOffer.Sign("Jane Candidate", DateTimeOffset.UnixEpoch);
        var signed = OfferLetterHtml.Render(OfferDto.FromDomain(signedOffer), "Illumin360");
        signed.Should().Contain("Jane Candidate");
        signed.Should().Contain("Signed electronically on");
    }

    [Fact]
    public void Letter_escapes_html()
    {
        var raw = Offer.Draft(Guid.NewGuid(), "<b>Dev</b>", 1m, "NAD", Start, null, DateTimeOffset.UnixEpoch).Value!;
        var html = OfferLetterHtml.Render(OfferDto.FromDomain(raw), "Illumin360");
        html.Should().NotContain("<b>Dev</b>");
        html.Should().Contain("&lt;b&gt;Dev&lt;/b&gt;");
    }
}
