using System.Text.Json;
using FluentAssertions;
using Illumin360.Recruitment.Application.Recruitment;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class CareersSyndicationTests
{
    private static readonly Guid RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static IReadOnlyList<RecruitmentRequestDto> Roles() =>
    [
        new RecruitmentRequestDto(RoleId, Guid.NewGuid(), "Backend Engineer & Co", "Windhoek", 2, "open", new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero), null),
    ];

    [Fact]
    public void Rss_has_channel_and_item_with_absolute_link()
    {
        var xml = CareersSyndication.RenderRss(Roles(), "Illumin360", "https://jobs.example.na/", "/careers");

        xml.Should().StartWith("<?xml");
        xml.Should().Contain("<rss version=\"2.0\">").And.Contain("<channel>");
        xml.Should().Contain($"<link>https://jobs.example.na/careers/{RoleId}</link>");
        // XML-escaped ampersand in the title.
        xml.Should().Contain("Backend Engineer &amp; Co");
        xml.Should().Contain("<pubDate>");
    }

    [Fact]
    public void Sitemap_lists_index_and_each_role()
    {
        var xml = CareersSyndication.RenderSitemap(Roles(), "https://jobs.example.na", "/careers");

        xml.Should().Contain("<urlset");
        xml.Should().Contain("<loc>https://jobs.example.na/careers</loc>");
        xml.Should().Contain($"<loc>https://jobs.example.na/careers/{RoleId}</loc>");
        xml.Should().Contain("<lastmod>2026-08-01</lastmod>");
    }

    [Fact]
    public void JsonFeed_is_valid_json_with_absolute_urls()
    {
        var json = CareersSyndication.RenderJsonFeed(Roles(), "https://jobs.example.na", "/careers");

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetArrayLength().Should().Be(1);
        var item = doc.RootElement[0];
        item.GetProperty("title").GetString().Should().Be("Backend Engineer & Co");
        item.GetProperty("url").GetString().Should().Be($"https://jobs.example.na/careers/{RoleId}");
        item.GetProperty("positions").GetInt32().Should().Be(2);
    }
}
