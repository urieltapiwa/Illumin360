namespace Illumin360.Resume;

/// <summary>
/// Detects known skills in resume text by matching against a vocabulary. Deterministic and
/// case-insensitive; multi-word skills (e.g. "machine learning") are matched as phrases, single-word
/// skills as whole tokens so "java" does not match inside "javascript".
/// </summary>
public static class SkillExtractor
{
    /// <summary>A reasonable default technical/professional skill vocabulary for the demo.</summary>
    public static readonly IReadOnlyList<string> DefaultVocabulary =
    [
        "Python", "Java", "JavaScript", "TypeScript", "SQL", "PostgreSQL", "MySQL",
        "React", "Angular", "Vue", "Node.js", "HTML", "CSS", "Azure", "AWS", "Docker", "Kubernetes",
        "Git", "Linux", "REST", "GraphQL", "Machine Learning", "Data Analysis", "Excel", "Power BI",
        "Networking", "Cybersecurity", "Accounting", "Project Management", "Communication",
        "Problem Solving", "Leadership", "Databases", "DevOps", "Agile", "Scrum",
    ];

    /// <summary>Detects which vocabulary skills appear in the given text.</summary>
    /// <param name="text">Resume text.</param>
    /// <param name="vocabulary">Skill vocabulary (defaults to <see cref="DefaultVocabulary"/>).</param>
    /// <returns>The detected skills, in vocabulary order, de-duplicated.</returns>
    public static IReadOnlyList<string> Detect(string? text, IReadOnlyList<string>? vocabulary = null)
    {
        var vocab = vocabulary ?? DefaultVocabulary;
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var haystack = $" {Normalize(text)} ";
        var found = new List<string>();
        foreach (var skill in vocab)
        {
            var needle = $" {Normalize(skill)} ";
            if (haystack.Contains(needle, StringComparison.Ordinal) && !found.Contains(skill))
            {
                found.Add(skill);
            }
        }

        return found;
    }

    // Lowercase and collapse any run of non-alphanumeric characters to a single space, so punctuation
    // and layout don't defeat whole-token/phrase matching. "C#" → "c", ".NET" → "net", "Node.js" → "node js".
    private static string Normalize(string value)
    {
        var chars = new char[value.Length];
        var i = 0;
        var lastWasSpace = false;
        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c))
            {
                chars[i++] = char.ToLowerInvariant(c);
                lastWasSpace = false;
            }
            else if (!lastWasSpace)
            {
                chars[i++] = ' ';
                lastWasSpace = true;
            }
        }

        return new string(chars, 0, i).Trim();
    }
}
