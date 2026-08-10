using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using Illumin360.Resume;
using Xunit;

namespace Illumin360.Resume.UnitTests;

public class DocxExtractionTests
{
    /// <summary>Builds a minimal .docx in memory containing the given text.</summary>
    public static byte[] BuildDocx(string text)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body(new Paragraph(new Run(new Text(text)))));
            main.Document.Save();
        }

        return ms.ToArray();
    }

    private const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    [Fact]
    public void Extracts_text_from_docx_and_detects_skills()
    {
        using var stream = new MemoryStream(BuildDocx("Graduate developer skilled in Python, SQL and Docker."));

        var text = ResumeTextExtractor.Extract(stream, Docx);

        text.Should().Contain("Python");
        SkillExtractor.Detect(text).Should().Contain(["Python", "SQL", "Docker"]);
    }
}
