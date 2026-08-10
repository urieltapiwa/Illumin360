using System.Text;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace Illumin360.Resume;

/// <summary>Extracts plain text from an uploaded resume (PDF, DOCX, or plain text).</summary>
public static class ResumeTextExtractor
{
    /// <summary>Extracts text from a resume stream based on its content type.</summary>
    /// <param name="content">The resume content stream.</param>
    /// <param name="contentType">The MIME type (pdf / docx / text).</param>
    /// <returns>The extracted plain text (empty if nothing could be read).</returns>
    public static string Extract(Stream content, string contentType)
    {
        ArgumentNullException.ThrowIfNull(content);

        // Buffer to a seekable copy — PdfPig/OpenXml need random access, and the source may be a network stream.
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        buffer.Position = 0;

        return contentType switch
        {
            "application/pdf" => ExtractPdf(buffer),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ExtractDocx(buffer),
            _ => ExtractText(buffer),
        };
    }

    private static string ExtractPdf(Stream stream)
    {
        var sb = new StringBuilder();
        using var document = PdfDocument.Open(stream);
        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }

    private static string ExtractDocx(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, false);
        return document.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
    }

    private static string ExtractText(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return reader.ReadToEnd();
    }
}
