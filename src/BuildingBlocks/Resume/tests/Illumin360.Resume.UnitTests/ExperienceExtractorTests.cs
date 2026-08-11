using FluentAssertions;
using Illumin360.Resume;
using Xunit;

namespace Illumin360.Resume.UnitTests;

public class ExperienceExtractorTests
{
    private const string Cv = """
        Jane Candidate
        Windhoek · jane@example.na

        Summary
        Backend engineer with 6 years' experience.

        Work Experience
        Senior Software Developer at Namib Mills (2021 - Present)
        Software Developer, Etosha Fintech 2018–2021
        Intern | Acme 2017

        Education
        BSc Computer Science, University of Namibia (2014 - 2017)

        Skills
        C#, PostgreSQL 2024 refresher
        """;

    [Fact]
    public void Extracts_experience_entries_with_title_org_period()
    {
        var exp = ExperienceExtractor.ExtractExperience(Cv);

        exp.Should().HaveCount(3);
        exp[0].Title.Should().Be("Senior Software Developer");
        exp[0].Organization.Should().Be("Namib Mills");
        exp[0].Period.Should().Contain("2021");
        exp[1].Title.Should().Be("Software Developer");
        exp[1].Organization.Should().Be("Etosha Fintech");
        exp[2].Organization.Should().Be("Acme");
    }

    [Fact]
    public void Extracts_education_entries()
    {
        var edu = ExperienceExtractor.ExtractEducation(Cv);

        edu.Should().ContainSingle();
        edu[0].Title.Should().Be("BSc Computer Science");
        edu[0].Organization.Should().Be("University of Namibia");
        edu[0].Period.Should().Contain("2014");
    }

    [Fact]
    public void Section_boundaries_are_respected()
    {
        // The "2024" under Skills must not leak into experience/education.
        ExperienceExtractor.ExtractExperience(Cv).Should().OnlyContain(e => e.Period != null && !e.Period.Contains("2024"));
        ExperienceExtractor.ExtractEducation(Cv).Should().NotContain(e => e.Title.Contains("C#"));
    }

    [Fact]
    public void Empty_or_headingless_text_yields_nothing()
    {
        ExperienceExtractor.ExtractExperience("").Should().BeEmpty();
        ExperienceExtractor.ExtractExperience("just some prose with a 2020 year but no headings").Should().BeEmpty();
    }
}
