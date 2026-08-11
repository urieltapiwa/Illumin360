using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>
/// Renders the public, SEO-friendly branded careers pages (a listing and per-role detail) as
/// self-contained HTML. Output includes descriptive <c>&lt;title&gt;</c>/meta tags, Open Graph tags, a
/// canonical link and schema.org JSON-LD (ItemList / JobPosting) so search engines and job aggregators
/// can index the roles. All dynamic values are HTML- or JSON-encoded to prevent injection.
/// </summary>
public static class CareersHtml
{
    /// <summary>Renders the careers landing page listing the open roles.</summary>
    /// <param name="roles">The open roles to list.</param>
    /// <param name="brand">The brand/site name shown in the header and titles.</param>
    /// <param name="basePath">The public base path for links (e.g. <c>/careers</c>).</param>
    /// <returns>A complete HTML document.</returns>
    public static string RenderIndex(IReadOnlyList<RecruitmentRequestDto> roles, string brand, string basePath)
    {
        ArgumentNullException.ThrowIfNull(roles);
        var b = Enc(brand);
        var bp = TrimPath(basePath);
        var count = roles.Count;
        var title = $"Careers at {b} — {count} open role{(count == 1 ? string.Empty : "s")}";
        var description = count == 0
            ? $"There are no open roles at {b} right now. Check back soon."
            : $"Browse {count} open role{(count == 1 ? string.Empty : "s")} at {b} and apply today.";

        var jsonLd = JsonSerializer.Serialize(new
        {
            context = "https://schema.org",
            type = "ItemList",
            name = $"Open roles at {brand}",
            numberOfItems = count,
            itemListElement = roles.Select((r, i) => new
            {
                type = "ListItem",
                position = i + 1,
                url = $"{bp}/{r.Id}",
                name = r.Title,
            }),
        });

        var sb = new StringBuilder();
        AppendHead(sb, title, description, $"{bp}", jsonLd, $"{bp}/feed.xml");
        sb.Append("<body><main class=\"wrap\">");
        sb.Append(CultureInfo.InvariantCulture, $"<header class=\"hero\"><p class=\"eyebrow\">{b} Careers</p><h1>Open roles</h1><p class=\"lede\">{Enc(description)}</p></header>");

        if (count == 0)
        {
            sb.Append("<p class=\"empty\">No open roles right now — please check back soon.</p>");
        }
        else
        {
            sb.Append("<ul class=\"roles\">");
            foreach (var r in roles)
            {
                sb.Append(CultureInfo.InvariantCulture, $"<li class=\"role\"><a href=\"{bp}/{r.Id}\"><span class=\"title\">{Enc(r.Title)}</span><span class=\"meta\">{Enc(r.City)} · {r.Positions} position{(r.Positions == 1 ? string.Empty : "s")} · posted {r.CreatedAt.UtcDateTime.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}</span></a></li>");
            }

            sb.Append("</ul>");
        }

        sb.Append(CultureInfo.InvariantCulture, $"<footer class=\"foot\">{b} · Powered by Illumin360</footer>");
        sb.Append("</main></body></html>");
        return sb.ToString();
    }

    /// <summary>Renders a single role's detail page with a schema.org JobPosting.</summary>
    /// <param name="role">The role to render.</param>
    /// <param name="brand">The brand/site name.</param>
    /// <param name="basePath">The public base path for links (e.g. <c>/careers</c>).</param>
    /// <returns>A complete HTML document.</returns>
    public static string RenderJob(RecruitmentRequestDto role, string brand, string basePath)
    {
        ArgumentNullException.ThrowIfNull(role);
        var b = Enc(brand);
        var bp = TrimPath(basePath);
        var title = $"{Enc(role.Title)} — Careers at {b}";
        var description = $"{Enc(role.Title)} in {Enc(role.City)} at {b}. {role.Positions} position{(role.Positions == 1 ? string.Empty : "s")} open. Apply now.";

        var jsonLd = JsonSerializer.Serialize(new
        {
            context = "https://schema.org",
            type = "JobPosting",
            title = role.Title,
            datePosted = role.CreatedAt.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            employmentType = "FULL_TIME",
            hiringOrganization = new { type = "Organization", name = brand },
            jobLocation = new
            {
                type = "Place",
                address = new { type = "PostalAddress", addressLocality = role.City, addressCountry = "NA" },
            },
            totalJobOpenings = role.Positions,
        });

        var sb = new StringBuilder();
        AppendHead(sb, title, description, $"{bp}/{role.Id}", jsonLd);
        sb.Append("<body><main class=\"wrap\">");
        sb.Append(CultureInfo.InvariantCulture, $"<p class=\"back\"><a href=\"{bp}\">← All roles</a></p>");
        sb.Append(CultureInfo.InvariantCulture, $"<header class=\"hero\"><p class=\"eyebrow\">{b} Careers</p><h1>{Enc(role.Title)}</h1><p class=\"lede\">{Enc(role.City)} · {role.Positions} position{(role.Positions == 1 ? string.Empty : "s")} · posted {role.CreatedAt.UtcDateTime.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}</p></header>");
        sb.Append(CultureInfo.InvariantCulture, $"<section class=\"body\"><p>{b} is hiring a <strong>{Enc(role.Title)}</strong> based in {Enc(role.City)}. This is an open role on the Illumin360 marketplace.</p><p><a class=\"apply\" href=\"/?screen=register\">Apply on Illumin360</a></p></section>");

        // Social share row — hrefs are populated client-side from the page URL (no server origin needed).
        sb.Append("<section class=\"share\"><span>Share this role:</span> <a id=\"s-x\" rel=\"noopener\" target=\"_blank\">X</a> <a id=\"s-li\" rel=\"noopener\" target=\"_blank\">LinkedIn</a> <a id=\"s-fb\" rel=\"noopener\" target=\"_blank\">Facebook</a> <a id=\"s-em\">Email</a></section>");
        sb.Append("<script>(function(){var u=encodeURIComponent(location.href),t=encodeURIComponent(document.title);var m={'s-x':'https://twitter.com/intent/tweet?url='+u+'&text='+t,'s-li':'https://www.linkedin.com/sharing/share-offsite/?url='+u,'s-fb':'https://www.facebook.com/sharer/sharer.php?u='+u,'s-em':'mailto:?subject='+t+'&body='+u};for(var k in m){var e=document.getElementById(k);if(e)e.href=m[k];}})();</script>");
        sb.Append(CultureInfo.InvariantCulture, $"<footer class=\"foot\">{b} · Powered by Illumin360</footer>");
        sb.Append("</main></body></html>");
        return sb.ToString();
    }

    private static void AppendHead(StringBuilder sb, string title, string description, string canonicalPath, string jsonLd, string? feedPath = null)
    {
        var t = Enc(title);
        var d = Enc(description);
        sb.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.Append(CultureInfo.InvariantCulture, $"<title>{t}</title><meta name=\"description\" content=\"{d}\">");
        sb.Append(CultureInfo.InvariantCulture, $"<link rel=\"canonical\" href=\"{Enc(canonicalPath)}\">");
        if (!string.IsNullOrEmpty(feedPath))
        {
            sb.Append(CultureInfo.InvariantCulture, $"<link rel=\"alternate\" type=\"application/rss+xml\" title=\"Open roles\" href=\"{Enc(feedPath)}\">");
        }

        sb.Append(CultureInfo.InvariantCulture, $"<meta property=\"og:type\" content=\"website\"><meta property=\"og:title\" content=\"{t}\"><meta property=\"og:description\" content=\"{d}\">");
        sb.Append(CultureInfo.InvariantCulture, $"<script type=\"application/ld+json\">{jsonLd}</script>");
        sb.Append("<style>");
        sb.Append(":root{color-scheme:light dark}*{box-sizing:border-box}body{margin:0;font:16px/1.5 system-ui,-apple-system,Segoe UI,Roboto,sans-serif;background:#0b1411;color:#e8f2ec}");
        sb.Append(".wrap{max-width:760px;margin:0 auto;padding:48px 20px}.eyebrow{text-transform:uppercase;letter-spacing:.18em;font-size:12px;color:#2fd39a;margin:0 0 8px}");
        sb.Append("h1{font-size:34px;margin:0 0 8px;letter-spacing:-.02em}.lede{color:#9fb3aa;margin:0}.hero{margin-bottom:32px}");
        sb.Append(".roles{list-style:none;padding:0;margin:0;display:grid;gap:12px}.role a{display:flex;flex-direction:column;gap:4px;padding:16px 18px;border:1px solid #1c3329;border-radius:14px;text-decoration:none;color:inherit;background:#0f1c17;transition:border-color .15s}");
        sb.Append(".role a:hover{border-color:#2fd39a}.title{font-weight:600;font-size:17px}.meta{color:#9fb3aa;font-size:13px}");
        sb.Append(".empty{color:#9fb3aa}.back a{color:#2fd39a;text-decoration:none;font-size:14px}.body{margin-top:24px}");
        sb.Append(".apply{display:inline-block;margin-top:12px;background:#1fb283;color:#04120c;font-weight:700;padding:10px 18px;border-radius:10px;text-decoration:none}");
        sb.Append(".share{margin-top:24px;font-size:14px;color:#9fb3aa}.share a{color:#2fd39a;text-decoration:none;margin:0 6px}.share a:hover{text-decoration:underline}");
        sb.Append(".foot{margin-top:48px;padding-top:16px;border-top:1px solid #1c3329;color:#6f8479;font-size:13px}");
        sb.Append("</style></head>");
    }

    private static string Enc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string TrimPath(string basePath) => string.IsNullOrWhiteSpace(basePath) ? "/careers" : basePath.TrimEnd('/');
}
