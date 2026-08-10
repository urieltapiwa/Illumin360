using FluentAssertions;
using Illumin360.Recruitment.Application.Recruitment;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class CareersHtmlTests
{
    private static RecruitmentRequestDto Role(string title, string city, int positions = 2)
        => new(Guid.NewGuid(), Guid.NewGuid(), title, city, positions, "Open", DateTimeOffset.UnixEpoch, null);

    [Fact]
    public void Index_lists_roles_with_seo_metadata_and_jsonld()
    {
        var roles = new[] { Role("Software Developer", "Windhoek"), Role("Head Chef", "Swakopmund") };

        var html = CareersHtml.RenderIndex(roles, "Illumin360", "/careers");

        html.Should().StartWith("<!doctype html>");
        html.Should().Contain("<title>Careers at Illumin360 — 2 open roles</title>");
        html.Should().Contain("<link rel=\"canonical\" href=\"/careers\">");
        html.Should().Contain("application/ld+json");
        html.Should().Contain("ItemList");
        html.Should().Contain("Software Developer");
        html.Should().Contain("href=\"/careers/" + roles[0].Id + "\"");
    }

    [Fact]
    public void Index_handles_empty_list()
    {
        var html = CareersHtml.RenderIndex([], "Illumin360", "/careers");

        html.Should().Contain("no open roles");
        html.Should().Contain("\"numberOfItems\":0");
    }

    [Fact]
    public void Job_page_emits_jobposting_jsonld()
    {
        var role = Role("Data Engineer", "Windhoek", 1);

        var html = CareersHtml.RenderJob(role, "Illumin360", "/careers");

        html.Should().Contain("<title>Data Engineer — Careers at Illumin360</title>");
        html.Should().Contain("JobPosting");
        html.Should().Contain("\"datePosted\":\"1970-01-01\"");
        html.Should().Contain("Windhoek");
        html.Should().Contain("1 position ·"); // singular
        html.Should().Contain("href=\"/careers\""); // back link
    }

    [Fact]
    public void Renderer_escapes_html_in_titles()
    {
        var role = Role("<script>alert(1)</script> Dev", "Windhoek");

        var html = CareersHtml.RenderJob(role, "Illumin360", "/careers");

        html.Should().NotContain("<script>alert(1)</script>");
        html.Should().Contain("&lt;script&gt;");
    }
}
