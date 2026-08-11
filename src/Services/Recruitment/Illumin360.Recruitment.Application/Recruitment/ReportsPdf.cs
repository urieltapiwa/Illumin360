using System.Globalization;
using System.Text;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>
/// Renders a simple, single-page text report as a self-contained PDF (no external dependency). Produces a
/// minimal but valid PDF 1.4 document: catalog, one A4 page, a Helvetica font, and a content stream that
/// draws a title and one line per row. Text is ASCII-sanitised and PDF-escaped.
/// </summary>
public static class ReportsPdf
{
    private static readonly Encoding Latin1 = Encoding.Latin1;

    /// <summary>Renders a title + lines into PDF bytes.</summary>
    /// <param name="title">The report title.</param>
    /// <param name="lines">The report body lines (one draw per line).</param>
    /// <returns>The PDF document bytes.</returns>
    public static byte[] Render(string title, IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(lines);

        // --- Content stream: draw the title, then each line 16pt below the previous. ---
        var content = new StringBuilder();
        content.Append("BT\n/F1 16 Tf\n50 800 Td\n");
        content.Append(CultureInfo.InvariantCulture, $"({Escape(title)}) Tj\n");
        content.Append("/F1 11 Tf\n");
        foreach (var line in lines)
        {
            content.Append(CultureInfo.InvariantCulture, $"0 -16 Td\n({Escape(line)}) Tj\n");
        }

        content.Append("ET");
        var contentBytes = Latin1.GetBytes(content.ToString());

        // --- Assemble objects, tracking byte offsets for the xref table. ---
        var pdf = new StringBuilder();
        var offsets = new List<int>();
        void Obj(string body)
        {
            offsets.Add(Latin1.GetByteCount(pdf.ToString()));
            pdf.Append(body);
        }

        pdf.Append("%PDF-1.4\n");
        Obj("1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n");
        Obj("2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n");
        Obj("3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 595 842]/Resources<</Font<</F1 5 0 R>>>>/Contents 4 0 R>>endobj\n");
        Obj(string.Create(CultureInfo.InvariantCulture, $"4 0 obj<</Length {contentBytes.Length}>>stream\n{content}\nendstream endobj\n"));
        Obj("5 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\n");

        var xrefOffset = Latin1.GetByteCount(pdf.ToString());
        pdf.Append("xref\n0 6\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            pdf.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        pdf.Append("trailer<</Size 6/Root 1 0 R>>\nstartxref\n");
        pdf.Append(CultureInfo.InvariantCulture, $"{xrefOffset}\n%%EOF");

        return Latin1.GetBytes(pdf.ToString());
    }

    // Drop non-ASCII (Helvetica base encoding) and escape the PDF string delimiters.
    private static string Escape(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (ch is '(' or ')' or '\\')
            {
                sb.Append('\\').Append(ch);
            }
            else if (ch is >= ' ' and <= '~')
            {
                sb.Append(ch);
            }
            else
            {
                sb.Append('?');
            }
        }

        return sb.ToString();
    }
}
