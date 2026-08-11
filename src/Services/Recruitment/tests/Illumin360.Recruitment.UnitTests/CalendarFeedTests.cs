using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class CalendarFeedTests
{
    private static Interview AnInterview(int day)
        => Interview.Schedule(Guid.NewGuid(), new DateTimeOffset(2026, 9, day, 9, 0, 0, TimeSpan.Zero), 60, "Video call", DateTimeOffset.UnixEpoch).Value!;

    [Fact]
    public void Feed_wraps_events_in_one_vcalendar()
    {
        var one = AnInterview(1);
        var two = AnInterview(2);
        var cancelled = AnInterview(3);
        cancelled.Cancel();

        var ics = Ics.BuildFeed("My interviews", [one, two, cancelled]);

        ics.Should().StartWith("BEGIN:VCALENDAR\r\n");
        ics.Should().EndWith("END:VCALENDAR\r\n");
        ics.Should().Contain("X-WR-CALNAME:My interviews");
        System.Text.RegularExpressions.Regex.Count(ics, "BEGIN:VEVENT").Should().Be(3);
        ics.Should().Contain("STATUS:CANCELLED"); // the cancelled one only
        ics.Should().Contain("DTSTART:20260901T090000Z");
    }

    [Fact]
    public void Empty_feed_is_still_valid()
    {
        var ics = Ics.BuildFeed("Empty", []);
        ics.Should().Contain("BEGIN:VCALENDAR");
        ics.Should().Contain("END:VCALENDAR");
        ics.Should().NotContain("BEGIN:VEVENT");
    }

    [Fact]
    public async Task Handler_builds_feed_from_talent_interviews()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.ListInterviewsForTalentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new[] { AnInterview(1) });
        var handler = new GetTalentCalendarFeedQueryHandler(repo);

        var result = await handler.HandleAsync(new GetTalentCalendarFeedQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().Contain("BEGIN:VEVENT");
    }
}
