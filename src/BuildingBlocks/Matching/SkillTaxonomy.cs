namespace Illumin360.Matching;

/// <summary>A canonical skill: a stable id plus a human display name.</summary>
/// <param name="Id">Stable lowercase identifier (e.g. <c>javascript</c>).</param>
/// <param name="Display">Human display name (e.g. <c>JavaScript</c>).</param>
public sealed record CanonicalSkill(string Id, string Display);

/// <summary>
/// Skills taxonomy v1 — a curated, dependency-free ontology that normalises and de-duplicates free-text
/// skills onto canonical entries via synonym/alias mapping (e.g. "JS", "ECMAScript" → JavaScript;
/// "reactjs" → React). Unknown skills pass through cleaned (nothing is lost) so it degrades gracefully.
/// This gives every service a single canonical vocabulary to match on, instead of raw strings.
/// </summary>
public static class SkillTaxonomy
{
    // Curated seed: canonical (id, display) → its aliases. Extend freely; a hosted/inferred ontology is a
    // later step behind the same API. Alias keys are compared in normalised form (see Normalize).
    private static readonly (string Id, string Display, string[] Aliases)[] Catalog =
    [
        ("javascript", "JavaScript", ["js", "ecmascript", "java script", "java-script"]),
        ("typescript", "TypeScript", ["ts"]),
        ("python", "Python", ["py", "python3"]),
        ("csharp", "C#", ["c sharp", "c-sharp", "cs", "dotnet c#"]),
        ("dotnet", ".NET", ["dot net", "net", "asp.net", "aspnet", ".net core", "dotnet core"]),
        ("java", "Java", ["java se", "core java"]),
        ("cpp", "C++", ["cplusplus", "c plus plus"]),
        ("react", "React", ["reactjs", "react.js", "react js"]),
        ("angular", "Angular", ["angularjs", "angular.js"]),
        ("vue", "Vue", ["vuejs", "vue.js"]),
        ("nodejs", "Node.js", ["node", "node js", "nodejs"]),
        ("sql", "SQL", ["structured query language"]),
        ("postgresql", "PostgreSQL", ["postgres", "psql", "postgre"]),
        ("mysql", "MySQL", ["my sql"]),
        ("mongodb", "MongoDB", ["mongo"]),
        ("aws", "AWS", ["amazon web services"]),
        ("azure", "Azure", ["microsoft azure", "ms azure"]),
        ("gcp", "GCP", ["google cloud", "google cloud platform"]),
        ("docker", "Docker", ["containers", "containerisation", "containerization"]),
        ("kubernetes", "Kubernetes", ["k8s", "kube"]),
        ("git", "Git", ["github", "gitlab", "version control"]),
        ("html", "HTML", ["html5"]),
        ("css", "CSS", ["css3", "scss", "sass"]),
        ("rest", "REST APIs", ["rest api", "restful", "rest apis", "web api", "web apis"]),
        ("graphql", "GraphQL", ["graph ql"]),
        ("machine-learning", "Machine Learning", ["ml", "machinelearning"]),
        ("project-management", "Project Management", ["pm", "project mgmt"]),
        ("communication", "Communication", ["communications", "verbal communication", "written communication"]),
        ("leadership", "Leadership", ["team leadership", "people leadership"]),
        ("customer-service", "Customer Service", ["customer support", "client service", "customer care"]),
        ("accounting", "Accounting", ["bookkeeping", "book keeping"]),
        ("excel", "Microsoft Excel", ["ms excel", "excel spreadsheets", "spreadsheets"]),
    ];

    private static readonly Dictionary<string, CanonicalSkill> AliasIndex = BuildIndex();

    /// <summary>Maps a raw skill string to its canonical skill; unknown skills pass through cleaned.</summary>
    /// <param name="raw">The raw skill text.</param>
    /// <returns>The canonical skill (never null for non-blank input; blanks yield an empty id).</returns>
    public static CanonicalSkill Canonicalize(string? raw)
    {
        var normalized = Normalize(raw);
        if (normalized.Length == 0)
        {
            return new CanonicalSkill(string.Empty, string.Empty);
        }

        if (AliasIndex.TryGetValue(normalized, out var known))
        {
            return known;
        }

        // Unknown skill: stable slug id + a tidied display (original trimmed, single-spaced).
        var display = string.Join(' ', (raw ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return new CanonicalSkill(normalized.Replace(' ', '-'), display);
    }

    /// <summary>The canonical id for a raw skill (empty for blank input).</summary>
    /// <param name="raw">The raw skill text.</param>
    public static string CanonicalId(string? raw) => Canonicalize(raw).Id;

    /// <summary>De-duplicates a list of raw skills onto canonical skills (first-seen order, blanks dropped).</summary>
    /// <param name="raws">The raw skills.</param>
    /// <returns>Distinct canonical skills.</returns>
    public static IReadOnlyList<CanonicalSkill> Dedupe(IEnumerable<string>? raws)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<CanonicalSkill>();
        foreach (var raw in raws ?? [])
        {
            var canonical = Canonicalize(raw);
            if (canonical.Id.Length > 0 && seen.Add(canonical.Id))
            {
                result.Add(canonical);
            }
        }

        return result;
    }

    /// <summary>
    /// Groups raw skills that collapse to the same canonical skill (e.g. a profile listing both "JS" and
    /// "JavaScript"), returning only groups with more than one raw member — the "you can merge these" hints.
    /// </summary>
    /// <param name="raws">The raw skills.</param>
    /// <returns>Duplicate groups: the canonical skill + the raw members that map to it.</returns>
    public static IReadOnlyList<SkillDuplicateGroup> DuplicateGroups(IEnumerable<string>? raws)
        => (raws ?? [])
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .GroupBy(Canonicalize, r => r.Trim())
            .Where(g => g.Key.Id.Length > 0 && g.Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .Select(g => new SkillDuplicateGroup(g.Key, g.Distinct(StringComparer.OrdinalIgnoreCase).ToList()))
            .ToList();

    // Lowercase, trim, collapse internal whitespace; keeps symbols like +, #, . intact (C++, C#, .NET).
    private static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var tokens = raw.Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', tokens);
    }

    private static Dictionary<string, CanonicalSkill> BuildIndex()
    {
        var index = new Dictionary<string, CanonicalSkill>(StringComparer.Ordinal);
        foreach (var (id, display, aliases) in Catalog)
        {
            var canonical = new CanonicalSkill(id, display);
            index[Normalize(display)] = canonical;
            index[Normalize(id)] = canonical;
            foreach (var alias in aliases)
            {
                index[Normalize(alias)] = canonical;
            }
        }

        return index;
    }
}

/// <summary>A set of raw skills that map to one canonical skill (candidates for merging).</summary>
/// <param name="Canonical">The canonical skill they share.</param>
/// <param name="Members">The distinct raw skill strings that collapse to it.</param>
public sealed record SkillDuplicateGroup(CanonicalSkill Canonical, IReadOnlyList<string> Members);
