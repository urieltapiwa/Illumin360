using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class ReferralsTests
{
    private static RecruitmentRequest ARequest()
        => RecruitmentRequest.Post(Guid.NewGuid(), "Backend Engineer", "Windhoek", 1).Value!;

    [Fact]
    public void Referral_validates_names_and_candidate_email()
    {
        var req = Guid.NewGuid();
        Referral.Create(req, "", null, "Sam Cand", "sam@acme.na", null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        Referral.Create(req, "Rita Ref", null, "", "sam@acme.na", null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        Referral.Create(req, "Rita Ref", null, "Sam Cand", "not-an-email", null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        Referral.Create(req, "Rita Ref", "bad", "Sam Cand", "sam@acme.na", null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();

        var ok = Referral.Create(req, " Rita Ref ", " Rita@Acme.NA ", "Sam Cand", " Sam@Acme.NA ", "  worked together  ", DateTimeOffset.UnixEpoch);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.ReferrerEmail.Should().Be("rita@acme.na");
        ok.Value!.CandidateEmail.Should().Be("sam@acme.na");
        ok.Value!.Note.Should().Be("worked together");
    }

    [Fact]
    public async Task Submit_to_missing_request_is_not_found()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetByIdAsync(Arg.Any<RequestId>(), Arg.Any<CancellationToken>()).Returns((RecruitmentRequest?)null);
        var handler = new SubmitReferralCommandHandler(repo);

        var result = await handler.HandleAsync(new SubmitReferralCommand(Guid.NewGuid(), "Rita", null, "Sam", "sam@acme.na", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        repo.DidNotReceive().AddReferral(Arg.Any<Referral>());
    }

    [Fact]
    public async Task Submit_persists_referral()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetByIdAsync(Arg.Any<RequestId>(), Arg.Any<CancellationToken>()).Returns(ARequest());
        var handler = new SubmitReferralCommandHandler(repo);

        var result = await handler.HandleAsync(new SubmitReferralCommand(Guid.NewGuid(), "Rita", "rita@acme.na", "Sam", "sam@acme.na", "great dev"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CandidateName.Should().Be("Sam");
        repo.Received(1).AddReferral(Arg.Any<Referral>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_internal_creates_detail_when_absent_and_flags_it()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        var requestId = Guid.NewGuid();
        repo.GetByIdAsync(Arg.Any<RequestId>(), Arg.Any<CancellationToken>()).Returns(ARequest());
        repo.GetRequisitionDetailAsync(requestId, Arg.Any<CancellationToken>()).Returns((RequisitionDetail?)null);
        repo.ListRequisitionTagsAsync(requestId, Arg.Any<CancellationToken>()).Returns([]);
        var handler = new SetRequisitionInternalCommandHandler(repo);

        var result = await handler.HandleAsync(new SetRequisitionInternalCommand(requestId, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repo.Received(1).AddRequisitionDetail(Arg.Is<RequisitionDetail>(d => d.Internal));
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
