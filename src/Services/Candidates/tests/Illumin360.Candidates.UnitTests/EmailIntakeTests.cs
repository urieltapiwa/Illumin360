using System.Text;
using FluentAssertions;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;
using Illumin360.Storage;
using NSubstitute;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class EmailIntakeTests
{
    [Fact]
    public void DeriveName_prefers_display_name_then_filename_with_placeholders()
    {
        EmailIntake.DeriveName("Jane Doe", null).Should().Be(("Jane", "Doe"));
        EmailIntake.DeriveName("Jane van der Merwe", null).Should().Be(("Jane", "van der Merwe"));
        // Falls back to the file name (extension + separators stripped).
        EmailIntake.DeriveName(null, "john_roe_cv.pdf").Should().Be(("john", "roe cv"));
        // Single token → unknown last-name placeholder.
        EmailIntake.DeriveName("Madonna", null).Should().Be(("Madonna", "(unknown)"));
    }

    [Fact]
    public void BuildHeadline_uses_skills_then_subject_and_caps_length()
    {
        EmailIntake.BuildHeadline(["Go", "SQL"], "Application").Should().Be("Go, SQL");
        EmailIntake.BuildHeadline([], "Senior Dev application").Should().Be("Senior Dev application");
        EmailIntake.BuildHeadline([], "   ").Should().BeNull();
        EmailIntake.BuildHeadline([], new string('x', 200))!.Length.Should().Be(150);
    }

    [Fact]
    public async Task Ingest_creates_candidate_from_plaintext_resume()
    {
        var repo = Substitute.For<ICandidateRepository>();
        var storage = Substitute.For<IObjectStorage>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        var handler = new IngestEmailResumeCommandHandler(repo, storage, publisher);

        var resume = Convert.ToBase64String(Encoding.UTF8.GetBytes("Experienced engineer skilled in C# and PostgreSQL."));
        var result = await handler.HandleAsync(
            new IngestEmailResumeCommand("Jane Doe", "jane@x.na", "Application", "jane_cv.txt", resume, "text/plain"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Created.Should().BeTrue();
        result.Value!.CandidateName.Should().Be("Jane Doe");
        repo.Received(1).Add(Arg.Is<Candidate>(c => c.FirstName == "Jane" && c.LastName == "Doe"));
        // text/plain isn't a stored CV type, so storage is not written for it.
        await storage.DidNotReceive().PutAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ingest_skips_a_duplicate_resend()
    {
        var repo = Substitute.For<ICandidateRepository>();
        var storage = Substitute.For<IObjectStorage>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        // An existing stub with the same derived name + "Unknown" city.
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            Candidate.Register("Jane", "Doe", EmailIntake.UnknownField, EmailIntake.UnknownField).Value!,
        });
        var handler = new IngestEmailResumeCommandHandler(repo, storage, publisher);

        var result = await handler.HandleAsync(
            new IngestEmailResumeCommand("Jane Doe", "jane@x.na", "Application", null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Created.Should().BeFalse();
        result.Value!.Skipped.Should().Be("duplicate");
        repo.DidNotReceive().Add(Arg.Any<Candidate>());
    }
}
