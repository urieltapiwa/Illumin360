using FluentAssertions;
using Illumin360.Candidates.Application.Candidates;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class BlindRedactorTests
{
    private static readonly Guid Id = Guid.Parse("7f3abcde-0000-0000-0000-000000000000");

    [Fact]
    public void Label_is_stable_and_derived_from_id()
    {
        BlindRedactor.Label(Id).Should().Be("Candidate 7F3A");
        BlindRedactor.Label(Id).Should().Be(BlindRedactor.Label(Id)); // deterministic
    }

    [Fact]
    public void Redact_anonymises_name_and_nationality_but_keeps_job_relevant_fields()
    {
        var dto = new CandidateDto(Id, "Jane", "Doe", "Windhoek", "Namibian", "ActivelyLooking", "Backend developer");

        var redacted = BlindRedactor.Redact(dto);

        redacted.FirstName.Should().Be("Candidate 7F3A");
        redacted.LastName.Should().BeEmpty();
        redacted.Nationality.Should().Be("—");
        // Job-relevant signals preserved for unbiased assessment.
        redacted.City.Should().Be("Windhoek");
        redacted.Availability.Should().Be("ActivelyLooking");
        redacted.PublicHeadline.Should().Be("Backend developer");
        redacted.Id.Should().Be(Id); // id retained so recruiters can still act
    }
}
