using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class EngagementReviewTests
{
    private static readonly Guid AppId = Guid.NewGuid();

    private static RecruitmentApplication HiredApp()
    {
        var app = RecruitmentApplication.Apply(new RequestId(Guid.NewGuid()), Guid.NewGuid(), "professional", DateTimeOffset.UnixEpoch);
        app.Advance(DateTimeOffset.UnixEpoch); // reviewed
        app.Advance(DateTimeOffset.UnixEpoch); // shortlisted
        app.Advance(DateTimeOffset.UnixEpoch); // hired
        return app;
    }

    private static IRecruitmentRepository RepoFor(RecruitmentApplication app)
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetApplicationAsync(Arg.Any<Domain.ApplicationId>(), Arg.Any<CancellationToken>()).Returns(app);
        return repo;
    }

    [Fact]
    public async Task Cannot_review_an_application_that_was_not_hired()
    {
        var app = RecruitmentApplication.Apply(new RequestId(Guid.NewGuid()), Guid.NewGuid(), "professional", DateTimeOffset.UnixEpoch);
        var handler = new LeaveReviewCommandHandler(RepoFor(app));

        var result = await handler.HandleAsync(new LeaveReviewCommand(AppId, "employer", 5, "Great"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("review.not_hired");
    }

    [Fact]
    public async Task First_review_stays_hidden_until_the_counterparty_reviews()
    {
        var app = HiredApp();
        var repo = RepoFor(app);
        repo.GetReviewAsync(AppId, ReviewerSide.Employer, Arg.Any<CancellationToken>()).Returns((EngagementReview?)null);
        repo.GetReviewAsync(AppId, ReviewerSide.Talent, Arg.Any<CancellationToken>()).Returns((EngagementReview?)null);
        var handler = new LeaveReviewCommandHandler(repo);

        var result = await handler.HandleAsync(new LeaveReviewCommand(AppId, "employer", 5, "Great hire"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repo.Received(1).AddEngagementReview(Arg.Is<EngagementReview>(r => !r.Visible));
    }

    [Fact]
    public async Task Second_review_reveals_both_sides()
    {
        var app = HiredApp();
        var repo = RepoFor(app);
        var employerReview = EngagementReview.Create(AppId, app.RequestId.Value, app.TalentId, ReviewerSide.Employer, 4, "Solid", DateTimeOffset.UnixEpoch).Value!;
        repo.GetReviewAsync(AppId, ReviewerSide.Talent, Arg.Any<CancellationToken>()).Returns((EngagementReview?)null);
        repo.GetReviewAsync(AppId, ReviewerSide.Employer, Arg.Any<CancellationToken>()).Returns(employerReview);
        var handler = new LeaveReviewCommandHandler(repo);

        var result = await handler.HandleAsync(new LeaveReviewCommand(AppId, "talent", 5, "Great employer"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employerReview.Visible.Should().BeTrue(); // counterpart revealed
    }

    [Fact]
    public async Task A_side_cannot_review_twice()
    {
        var app = HiredApp();
        var repo = RepoFor(app);
        var existing = EngagementReview.Create(AppId, app.RequestId.Value, app.TalentId, ReviewerSide.Employer, 4, null, DateTimeOffset.UnixEpoch).Value!;
        repo.GetReviewAsync(AppId, ReviewerSide.Employer, Arg.Any<CancellationToken>()).Returns(existing);
        var handler = new LeaveReviewCommandHandler(repo);

        var result = await handler.HandleAsync(new LeaveReviewCommand(AppId, "employer", 5, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("review.already_left");
    }

    [Fact]
    public async Task Reputation_scores_only_visible_employer_reviews()
    {
        var talentId = Guid.NewGuid();
        var visible = EngagementReview.Create(Guid.NewGuid(), Guid.NewGuid(), talentId, ReviewerSide.Employer, 5, null, DateTimeOffset.UnixEpoch).Value!;
        visible.Reveal();
        var hidden = EngagementReview.Create(Guid.NewGuid(), Guid.NewGuid(), talentId, ReviewerSide.Employer, 1, null, DateTimeOffset.UnixEpoch).Value!; // not revealed
        var talentSide = EngagementReview.Create(Guid.NewGuid(), Guid.NewGuid(), talentId, ReviewerSide.Talent, 1, null, DateTimeOffset.UnixEpoch).Value!;
        talentSide.Reveal();

        var repo = Substitute.For<IRecruitmentRepository>();
        repo.ListReviewsForTalentAsync(talentId, Arg.Any<CancellationToken>()).Returns(new[] { visible, hidden, talentSide });
        var handler = new GetTalentReputationQueryHandler(repo);

        var result = await handler.HandleAsync(new GetTalentReputationQuery(talentId), CancellationToken.None);

        result.Value!.Count.Should().Be(1); // only the visible employer review
        result.Value!.Average.Should().Be(5);
    }
}
