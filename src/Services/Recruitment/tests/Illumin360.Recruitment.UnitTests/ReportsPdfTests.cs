using System.Text;
using FluentAssertions;
using Illumin360.Recruitment.Application.Recruitment;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class ReportsPdfTests
{
    private static string AsText(byte[] pdf) => Encoding.Latin1.GetString(pdf);

    [Fact]
    public void Produces_a_valid_pdf_envelope()
    {
        var pdf = ReportsPdf.Render("Report", ["line one", "line two"]);
        var text = AsText(pdf);

        text.Should().StartWith("%PDF-1.4");
        text.Should().EndWith("%%EOF");
        text.Should().Contain("/Type/Catalog");
        text.Should().Contain("startxref");
        text.Should().Contain("(Report) Tj");
        text.Should().Contain("(line one) Tj");
    }

    [Fact]
    public void Content_length_matches_the_stream_bytes()
    {
        var pdf = ReportsPdf.Render("T", ["a", "b"]);
        var text = AsText(pdf);

        // Extract declared /Length and the actual bytes between stream/endstream.
        var lengthToken = "/Length ";
        var li = text.IndexOf(lengthToken, StringComparison.Ordinal);
        var declared = int.Parse(new string(text[(li + lengthToken.Length)..].TakeWhile(char.IsDigit).ToArray()), System.Globalization.CultureInfo.InvariantCulture);

        var start = text.IndexOf("stream\n", StringComparison.Ordinal) + "stream\n".Length;
        var end = text.IndexOf("\nendstream", StringComparison.Ordinal);
        (end - start).Should().Be(declared);
    }

    [Fact]
    public void Escapes_parentheses_and_drops_non_ascii()
    {
        var pdf = ReportsPdf.Render("Title (X) é", ["ok"]);
        var text = AsText(pdf);

        text.Should().Contain(@"(Title \(X\) ?) Tj");
    }
}
