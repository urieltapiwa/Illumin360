using FluentAssertions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class CareerViewsAndFilterTests
{
    private static readonly Guid RemoteRole = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static RecruitmentRequestDto Role(Guid id, string title, string city) =>
        new(id, Guid.NewGuid(), title, city, 1, "open", new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero), null);

    [Fact]
    public void CareerView_first_then_records_increments()
    {
        var v = CareerView.First(Guid.NewGuid(), DateTimeOffset.UnixEpoch);
        v.Views.Should().Be(1);
        v.Record(DateTimeOffset.UnixEpoch.AddDays(1));
        v.Views.Should().Be(2);
        v.LastViewedAt.Should().Be(DateTimeOffset.UnixEpoch.AddDays(1));
    }

    [Fact]
    public void RenderIndex_shows_filter_form_and_remote_badge()
    {
        var featured = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var roles = new[] { Role(featured, "Backend Engineer", "Windhoek"), Role(RemoteRole, "Analyst", "Swakopmund") };
        var filter = new CareersHtml.CareersFilter("eng", true, new HashSet<Guid> { RemoteRole }, new HashSet<Guid> { featured });

        var html = CareersHtml.RenderIndex(roles, "Illumin360", "/careers", filter);

        html.Should().Contain("<form class=\"filter\"");
        html.Should().Contain("value=\"eng\"");           // keyword pre-filled
        html.Should().Contain("checked");                  // remote-only pre-checked
        html.Should().Contain("class=\"badge\">Remote");    // remote role badged
        html.Should().Contain("class=\"badge featured\">Featured"); // featured role badged
    }

    [Fact]
    public void RenderIndex_without_filter_has_no_form()
    {
        var html = CareersHtml.RenderIndex([Role(Guid.NewGuid(), "Analyst", "Windhoek")], "Illumin360", "/careers");
        html.Should().NotContain("<form class=\"filter\"");
    }
}
