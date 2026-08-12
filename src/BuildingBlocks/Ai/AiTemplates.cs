namespace Illumin360.Ai;

/// <summary>
/// Deterministic, dependency-free fallbacks for the writing-assistant tasks — used whenever the hosted model
/// is off, so the features always work (and CI stays offline). A hosted LLM produces richer output when
/// enabled; these guarantee a sensible baseline with zero egress.
/// </summary>
public static class AiTemplates
{
    /// <summary>Builds a structured job description from a title, city, and required skills.</summary>
    /// <param name="title">Role title.</param>
    /// <param name="city">Role city.</param>
    /// <param name="skills">Required skills.</param>
    /// <returns>A formatted job-description draft.</returns>
    public static string JobDescription(string title, string? city, IReadOnlyList<string>? skills)
    {
        var t = string.IsNullOrWhiteSpace(title) ? "the role" : title.Trim();
        var where = string.IsNullOrWhiteSpace(city) ? string.Empty : $" based in {city.Trim()}";
        var skillList = (skills ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();

        var lines = new List<string>
        {
            $"# {t}",
            string.Empty,
            $"We're hiring a {t}{where}. You'll take ownership of meaningful work, collaborate with a supportive team, and grow your craft.",
            string.Empty,
            "## What you'll do",
            $"- Deliver high-quality work as a {t}.",
            "- Collaborate across the team and communicate clearly.",
            "- Continuously improve how we work.",
            string.Empty,
            "## What we're looking for",
        };

        if (skillList.Count > 0)
        {
            lines.AddRange(skillList.Select(s => $"- Experience with {s}."));
        }
        else
        {
            lines.Add("- Relevant experience and a track record of delivery.");
        }

        lines.Add("- Strong communication and a collaborative mindset.");
        lines.Add(string.Empty);
        lines.Add("## What we offer");
        lines.Add("- A collaborative team, real ownership, and room to grow.");

        return string.Join('\n', lines);
    }

    /// <summary>Extractive summary: the first <paramref name="maxSentences"/> sentences plus a length note.</summary>
    /// <param name="text">The text to summarise.</param>
    /// <param name="maxSentences">How many leading sentences to keep.</param>
    /// <returns>A short summary.</returns>
    public static string Summarize(string? text, int maxSentences = 2)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var sentences = normalized
            .Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .ToList();

        var kept = sentences.Take(Math.Max(1, maxSentences)).ToList();
        var summary = string.Join(". ", kept);
        if (summary.Length > 0 && !summary.EndsWith('.'))
        {
            summary += ".";
        }

        var wordCount = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return sentences.Count > kept.Count ? $"{summary} (summarised from {wordCount} words)" : summary;
    }

    /// <summary>Drafts a short message for the given context + intent.</summary>
    /// <param name="context">Who/what the message is about (e.g. a candidate name + role).</param>
    /// <param name="intent">The message intent (e.g. "invite to interview", "reject kindly").</param>
    /// <returns>A drafted message.</returns>
    public static string DraftMessage(string? context, string? intent)
    {
        var ctx = string.IsNullOrWhiteSpace(context) ? "your application" : context.Trim();
        var purpose = string.IsNullOrWhiteSpace(intent) ? "follow up" : intent.Trim();

        var lines = new[]
        {
            "Hi,",
            string.Empty,
            $"Thanks for your interest — I'm reaching out regarding {ctx}.",
            $"I wanted to {purpose} and share the next steps with you.",
            "Please let me know a good time to connect, and feel free to reply with any questions.",
            string.Empty,
            "Best regards,",
            "The hiring team",
        };
        return string.Join('\n', lines);
    }
}
