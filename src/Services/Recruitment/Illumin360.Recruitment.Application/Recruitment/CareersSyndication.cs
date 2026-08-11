using System.Globalization;
using System.Security;
using System.Text;
using System.Text.Json;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>
/// Pure renderers for careers-feed syndication: an RSS 2.0 feed, an XML sitemap, and a JSON feed of the
/// open roles. Lets job aggregators, feed readers and search engines pick up postings. All dynamic
/// values are XML- or JSON-encoded. Absolute URLs are built from a caller-supplied site origin so links
/// resolve for external consumers.
/// </summary>
public static class CareersSyndication
{
    /// <summary>Renders an RSS 2.0 feed of the open roles.</summary>
    /// <param name="roles">The open roles (internal roles already excluded by the caller).</param>
    /// <param name="brand">Brand/site name.</param>
    /// <param name="siteOrigin">Absolute origin (e.g. <c>https://jobs.example.na</c>), no trailing slash.</param>
    /// <param name="basePath">Careers base path (e.g. <c>/careers</c>).</param>
    /// <returns>RSS 2.0 XML.</returns>
    public static string RenderRss(IReadOnlyList<RecruitmentRequestDto> roles, string brand, string siteOrigin, string basePath)
    {
        ArgumentNullException.ThrowIfNull(roles);
        var origin = TrimOrigin(siteOrigin);
        var bp = TrimPath(basePath);
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.Append("<rss version=\"2.0\"><channel>");
        sb.Append(CultureInfo.InvariantCulture, $"<title>{X(brand)} careers</title>");
        sb.Append(CultureInfo.InvariantCulture, $"<link>{X(origin + bp)}</link>");
        sb.Append(CultureInfo.InvariantCulture, $"<description>Open roles at {X(brand)}</description>");
        foreach (var r in roles)
        {
            var url = $"{origin}{bp}/{r.Id}";
            sb.Append("<item>");
            sb.Append(CultureInfo.InvariantCulture, $"<title>{X(r.Title)}</title>");
            sb.Append(CultureInfo.InvariantCulture, $"<link>{X(url)}</link>");
            sb.Append(CultureInfo.InvariantCulture, $"<guid isPermaLink=\"true\">{X(url)}</guid>");
            sb.Append(CultureInfo.InvariantCulture, $"<pubDate>{r.CreatedAt.UtcDateTime.ToString("r", CultureInfo.InvariantCulture)}</pubDate>");
            sb.Append(CultureInfo.InvariantCulture, $"<description>{X($"{r.Title} in {r.City} — {r.Positions} position(s) open.")}</description>");
            sb.Append("</item>");
        }

        sb.Append("</channel></rss>");
        return sb.ToString();
    }

    /// <summary>Renders an XML sitemap (careers index + each role).</summary>
    /// <param name="roles">The open roles (internal roles already excluded by the caller).</param>
    /// <param name="siteOrigin">Absolute origin, no trailing slash.</param>
    /// <param name="basePath">Careers base path.</param>
    /// <returns>Sitemap XML.</returns>
    public static string RenderSitemap(IReadOnlyList<RecruitmentRequestDto> roles, string siteOrigin, string basePath)
    {
        ArgumentNullException.ThrowIfNull(roles);
        var origin = TrimOrigin(siteOrigin);
        var bp = TrimPath(basePath);
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        sb.Append(CultureInfo.InvariantCulture, $"<url><loc>{X(origin + bp)}</loc></url>");
        foreach (var r in roles)
        {
            sb.Append("<url>");
            sb.Append(CultureInfo.InvariantCulture, $"<loc>{X($"{origin}{bp}/{r.Id}")}</loc>");
            sb.Append(CultureInfo.InvariantCulture, $"<lastmod>{r.CreatedAt.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}</lastmod>");
            sb.Append("</url>");
        }

        sb.Append("</urlset>");
        return sb.ToString();
    }

    /// <summary>Renders a JSON feed (array of role objects with absolute URLs).</summary>
    /// <param name="roles">The open roles (internal roles already excluded by the caller).</param>
    /// <param name="siteOrigin">Absolute origin, no trailing slash.</param>
    /// <param name="basePath">Careers base path.</param>
    /// <returns>A JSON array string.</returns>
    public static string RenderJsonFeed(IReadOnlyList<RecruitmentRequestDto> roles, string siteOrigin, string basePath)
    {
        ArgumentNullException.ThrowIfNull(roles);
        var origin = TrimOrigin(siteOrigin);
        var bp = TrimPath(basePath);
        var items = roles.Select(r => new
        {
            id = r.Id,
            title = r.Title,
            city = r.City,
            positions = r.Positions,
            postedAt = r.CreatedAt.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            url = $"{origin}{bp}/{r.Id}",
        });
        return JsonSerializer.Serialize(items);
    }

    private static string X(string? value) => SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;

    private static string TrimPath(string basePath) => string.IsNullOrWhiteSpace(basePath) ? "/careers" : basePath.TrimEnd('/');

    private static string TrimOrigin(string? origin) => string.IsNullOrWhiteSpace(origin) ? string.Empty : origin.TrimEnd('/');
}
