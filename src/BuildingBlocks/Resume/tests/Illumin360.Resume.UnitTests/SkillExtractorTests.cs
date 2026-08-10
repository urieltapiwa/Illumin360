using System.Text;
using FluentAssertions;
using Illumin360.Resume;
using Xunit;

namespace Illumin360.Resume.UnitTests;

public class SkillExtractorTests
{
    [Fact]
    public void Detects_present_skills()
    {
        var skills = SkillExtractor.Detect("Experienced in Python and PostgreSQL, built apps with React.");

        skills.Should().Contain(["Python", "PostgreSQL", "React"]);
    }

    [Fact]
    public void Does_not_match_java_inside_javascript()
    {
        var skills = SkillExtractor.Detect("Frontend engineer specialising in JavaScript.");

        skills.Should().Contain("JavaScript");
        skills.Should().NotContain("Java");
    }

    [Fact]
    public void Matches_multi_word_skills_as_phrases()
    {
        SkillExtractor.Detect("Focus on machine learning and data analysis.")
            .Should().Contain(["Machine Learning", "Data Analysis"]);
    }

    [Fact]
    public void Empty_text_yields_no_skills()
    {
        SkillExtractor.Detect("   ").Should().BeEmpty();
    }

    [Fact]
    public void PlainText_extraction_round_trips()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Senior Python developer, SQL and Docker."));

        var text = ResumeTextExtractor.Extract(stream, "text/plain");

        text.Should().Contain("Python");
        SkillExtractor.Detect(text).Should().Contain(["Python", "SQL", "Docker"]);
    }
}
