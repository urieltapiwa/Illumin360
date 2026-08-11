using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class MultiRoundInterviewsTests
{
    private static Interview ARound(string round, params string[] skills)
        => Interview.Schedule(Guid.NewGuid(), new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero), 60, "Video", DateTimeOffset.UnixEpoch, round, skills).Value!;

    [Fact]
    public void Schedule_captures_round_and_normalised_skills()
    {
        var iv = ARound("Technical", "Go", " SQL ", "go");
        iv.Round.Should().Be("Technical");
        iv.RequiredSkills.Should().BeEquivalentTo("go", "sql"); // trimmed, lower-cased, de-duped
    }

    [Fact]
    public void SkillRating_validates_range_and_skill()
    {
        InterviewSkillRating.Create(Guid.NewGuid(), "go", 0, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        InterviewSkillRating.Create(Guid.NewGuid(), "go", 6, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        InterviewSkillRating.Create(Guid.NewGuid(), "  ", 4, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();

        var ok = InterviewSkillRating.Create(Guid.NewGuid(), " Go ", 4, DateTimeOffset.UnixEpoch);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.Skill.Should().Be("go");
    }

    [Fact]
    public async Task Record_ratings_replaces_prior_and_rejects_bad_rating()
    {
        var interviewId = Guid.NewGuid();
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetInterviewAsync(Arg.Any<InterviewId>(), Arg.Any<CancellationToken>()).Returns(ARound("Tech", "go"));
        var prior = InterviewSkillRating.Create(interviewId, "old", 2, DateTimeOffset.UnixEpoch).Value!;
        repo.ListSkillRatingsTrackedAsync(interviewId, Arg.Any<CancellationToken>()).Returns(new[] { prior });
        var handler = new RecordSkillRatingsCommandHandler(repo);

        // Out-of-range rating fails the whole submit (nothing persisted).
        var bad = await handler.HandleAsync(new RecordSkillRatingsCommand(interviewId, [new SkillRatingInput("go", 9)]), CancellationToken.None);
        bad.IsFailure.Should().BeTrue();

        var ok = await handler.HandleAsync(new RecordSkillRatingsCommand(interviewId, [new SkillRatingInput("go", 4), new SkillRatingInput("sql", 3)]), CancellationToken.None);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.Should().HaveCount(2);
        repo.Received(1).RemoveSkillRating(prior);
        repo.Received(2).AddSkillRating(Arg.Any<InterviewSkillRating>());
    }

    [Fact]
    public async Task Summary_aggregates_skill_averages_across_rounds()
    {
        var appId = Guid.NewGuid();
        var r1 = ARound("Screen", "go");
        var r2 = ARound("Tech", "go", "sql");
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.ListInterviewsForApplicationAsync(appId, Arg.Any<CancellationToken>()).Returns(new[] { r1, r2 });
        repo.ListSkillRatingsForInterviewsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[]
        {
            InterviewSkillRating.Create(r1.Id.Value, "go", 4, DateTimeOffset.UnixEpoch).Value!,
            InterviewSkillRating.Create(r2.Id.Value, "go", 5, DateTimeOffset.UnixEpoch).Value!,
            InterviewSkillRating.Create(r2.Id.Value, "sql", 3, DateTimeOffset.UnixEpoch).Value!,
        });
        var handler = new GetInterviewSummaryQueryHandler(repo);

        var result = await handler.HandleAsync(new GetInterviewSummaryQuery(appId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Rounds.Should().HaveCount(2);
        var go = result.Value!.SkillAverages.Single(s => s.Skill == "go");
        go.Average.Should().Be(4.5); // (4 + 5) / 2
        go.Count.Should().Be(2);
        // Ordered by average descending: go (4.5) before sql (3).
        result.Value!.SkillAverages[0].Skill.Should().Be("go");
    }
}
