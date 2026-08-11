using System.Text.RegularExpressions;

namespace Illumin360.Resume;

/// <summary>A parsed experience or education entry from a resume.</summary>
/// <param name="Title">The role title or qualification.</param>
/// <param name="Organization">The employer or institution, if detected.</param>
/// <param name="Period">The date/period text, if detected (e.g. "2019 - 2022").</param>
public sealed record ResumeEntry(string Title, string? Organization, string? Period);

/// <summary>
/// Heuristically extracts work-experience and education entries from resume text. Deterministic: it
/// locates the relevant section by heading, then reads each line carrying a year/date range as one entry,
/// splitting "Title at/—/, Organization" where present. Best-effort — resumes are free-form.
/// </summary>
public static partial class ExperienceExtractor
{
    private static readonly string[] ExperienceHeadings =
        ["work experience", "professional experience", "experience", "employment history", "employment", "work history"];

    private static readonly string[] EducationHeadings =
        ["education", "academic background", "qualifications", "academic qualifications"];

    // Any recognised heading ends the current section.
    private static readonly string[] AllHeadings =
        [.. ExperienceHeadings, .. EducationHeadings, "skills", "projects", "references", "interests", "summary", "profile", "certifications", "languages", "contact"];

    /// <summary>Extracts work-experience entries.</summary>
    /// <param name="text">Resume text.</param>
    /// <returns>Experience entries in document order (may be empty).</returns>
    public static IReadOnlyList<ResumeEntry> ExtractExperience(string? text) => Extract(text, ExperienceHeadings);

    /// <summary>Extracts education entries.</summary>
    /// <param name="text">Resume text.</param>
    /// <returns>Education entries in document order (may be empty).</returns>
    public static IReadOnlyList<ResumeEntry> ExtractEducation(string? text) => Extract(text, EducationHeadings);

    private static List<ResumeEntry> Extract(string? text, string[] headings)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var entries = new List<ResumeEntry>();
        var inSection = false;

        for (var index = 0; index < lines.Length; index++)
        {
            var raw = lines[index].Trim();
            if (raw.Length == 0)
            {
                continue;
            }

            var lower = raw.ToLowerInvariant();
            var heading = lower.TrimEnd(':').Trim();

            if (IsHeading(heading, headings))
            {
                inSection = true;
                continue;
            }

            if (IsHeading(heading, AllHeadings))
            {
                inSection = false;
                continue;
            }

            if (!inSection)
            {
                continue;
            }

            var period = PeriodRegex().Match(raw);
            if (!period.Success)
            {
                continue;
            }

            var rest = raw.Remove(period.Index, period.Length).Trim().Trim('|', '-', '–', '—', ',', '(', ')', ' ');
            if (rest.Length == 0 && index + 1 < lines.Length)
            {
                rest = lines[index + 1].Trim();
            }

            var (title, organization) = SplitTitleOrg(rest);
            entries.Add(new ResumeEntry(title, organization, period.Value.Trim()));
        }

        return entries;
    }

    private static bool IsHeading(string line, string[] headings)
        => line.Length <= 40 && Array.Exists(headings, h => line == h || line.StartsWith(h + " ", StringComparison.Ordinal));

    private static (string Title, string? Organization) SplitTitleOrg(string rest)
    {
        if (string.IsNullOrWhiteSpace(rest))
        {
            return ("(unspecified)", null);
        }

        foreach (var sep in new[] { " at ", " — ", " – ", " - ", ", ", " | " })
        {
            var i = rest.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
            if (i > 0)
            {
                var title = rest[..i].Trim();
                var org = rest[(i + sep.Length)..].Trim();
                return (title.Length == 0 ? rest.Trim() : title, org.Length == 0 ? null : org);
            }
        }

        return (rest.Trim(), null);
    }

    // A year (1900–2099) optionally followed by a range to another year / present / current.
    [GeneratedRegex(@"\b(19|20)\d{2}\b(\s*[-–—]{1,2}\s*((19|20)\d{2}|present|current|now)\b)?", RegexOptions.IgnoreCase, "en")]
    private static partial Regex PeriodRegex();
}
