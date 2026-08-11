using System.Text;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>
/// Pure, dependency-free CSV parser for bulk candidate import. Handles RFC-4180 quoting (quoted fields,
/// embedded commas/newlines, doubled quotes), a header row mapping columns by name, and reports per-row
/// problems without throwing. Unit-testable in isolation of EF / the web layer.
/// </summary>
public static class CandidateCsv
{
    /// <summary>A parsed candidate row (1-based source line for error reporting).</summary>
    /// <param name="Line">1-based line number in the source.</param>
    /// <param name="FirstName">Given name.</param>
    /// <param name="LastName">Family name.</param>
    /// <param name="City">City.</param>
    /// <param name="Nationality">Nationality.</param>
    /// <param name="Availability">Availability status name, if provided.</param>
    /// <param name="Headline">Public headline, if provided.</param>
    public sealed record Row(int Line, string FirstName, string LastName, string City, string Nationality, string? Availability, string? Headline);

    /// <summary>The outcome of parsing: the usable rows plus any header/row-level problems.</summary>
    /// <param name="Rows">Successfully mapped rows.</param>
    /// <param name="Errors">Header or row problems (human-readable).</param>
    public sealed record ParseResult(IReadOnlyList<Row> Rows, IReadOnlyList<string> Errors);

    private static readonly string[] FirstNames = ["firstname", "first name", "first", "given name"];
    private static readonly string[] LastNames = ["lastname", "last name", "last", "surname", "family name"];
    private static readonly string[] Cities = ["city", "town", "location"];
    private static readonly string[] Nationalities = ["nationality", "country"];
    private static readonly string[] Availabilities = ["availability", "status"];
    private static readonly string[] Headlines = ["headline", "public headline", "title"];

    /// <summary>Parses CSV text into candidate rows.</summary>
    /// <param name="csv">The raw CSV (first non-empty line is the header).</param>
    /// <returns>The parsed rows + any problems.</returns>
    public static ParseResult Parse(string? csv)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(csv))
        {
            return new ParseResult([], ["The CSV is empty."]);
        }

        var records = SplitRecords(csv);
        if (records.Count == 0)
        {
            return new ParseResult([], ["The CSV has no rows."]);
        }

        var header = records[0].Fields.Select(h => h.Trim().ToLowerInvariant()).ToList();
        int Col(string[] names) => header.FindIndex(h => Array.Exists(names, n => n == h));

        var iFirst = Col(FirstNames);
        var iLast = Col(LastNames);
        var iCity = Col(Cities);
        var iNat = Col(Nationalities);
        var iAvail = Col(Availabilities);
        var iHead = Col(Headlines);

        if (iFirst < 0 || iLast < 0 || iCity < 0 || iNat < 0)
        {
            errors.Add("The header must include firstName, lastName, city and nationality columns.");
            return new ParseResult([], errors);
        }

        var rows = new List<Row>();
        for (var r = 1; r < records.Count; r++)
        {
            var fields = records[r].Fields;
            var line = records[r].Line;

            // Skip fully-blank rows silently.
            if (fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            string At(int index) => index >= 0 && index < fields.Count ? fields[index].Trim() : string.Empty;

            var first = At(iFirst);
            var last = At(iLast);
            var city = At(iCity);
            var nat = At(iNat);

            if (first.Length == 0 || last.Length == 0 || city.Length == 0 || nat.Length == 0)
            {
                errors.Add($"Line {line}: missing a required field (firstName/lastName/city/nationality).");
                continue;
            }

            var avail = iAvail >= 0 ? At(iAvail) : string.Empty;
            var head = iHead >= 0 ? At(iHead) : string.Empty;
            rows.Add(new Row(line, first, last, city, nat, avail.Length == 0 ? null : avail, head.Length == 0 ? null : head));
        }

        return new ParseResult(rows, errors);
    }

    private sealed record Record(int Line, List<string> Fields);

    // A minimal RFC-4180 tokenizer: fields separated by commas, records by newlines, with double-quote
    // escaping ("" → "). Tracks the 1-based starting line of each record.
    private static List<Record> SplitRecords(string csv)
    {
        var records = new List<Record>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var line = 1;
        var recordStartLine = 1;
        var sawAny = false;

        void EndField()
        {
            fields.Add(field.ToString());
            field.Clear();
        }

        void EndRecord()
        {
            EndField();
            records.Add(new Record(recordStartLine, [.. fields]));
            fields.Clear();
            sawAny = false;
        }

        for (var i = 0; i < csv.Length; i++)
        {
            var c = csv[i];
            if (!sawAny && !inQuotes)
            {
                recordStartLine = line;
            }

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    if (c == '\n')
                    {
                        line++;
                    }

                    field.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    sawAny = true;
                    break;
                case ',':
                    EndField();
                    sawAny = true;
                    break;
                case '\r':
                    break;
                case '\n':
                    EndRecord();
                    line++;
                    break;
                default:
                    field.Append(c);
                    sawAny = true;
                    break;
            }
        }

        // Flush a trailing record with no final newline.
        if (field.Length > 0 || fields.Count > 0)
        {
            EndRecord();
        }

        return records;
    }
}
