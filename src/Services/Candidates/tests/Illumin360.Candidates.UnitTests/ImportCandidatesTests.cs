using FluentAssertions;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;
using NSubstitute;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class ImportCandidatesTests
{
    [Fact]
    public void Parse_maps_columns_by_header_name_case_insensitively()
    {
        const string csv = "First Name,LastName,City,Nationality,Availability,Headline\n" +
                           "Jane,Doe,Windhoek,Namibian,OpenToOpportunities,Backend dev\n";
        var result = CandidateCsv.Parse(csv);

        result.Errors.Should().BeEmpty();
        result.Rows.Should().ContainSingle();
        result.Rows[0].FirstName.Should().Be("Jane");
        result.Rows[0].Availability.Should().Be("OpenToOpportunities");
        result.Rows[0].Headline.Should().Be("Backend dev");
    }

    [Fact]
    public void Parse_handles_quoted_fields_with_commas_and_newlines()
    {
        const string csv = "firstName,lastName,city,nationality,headline\n" +
                           "Sam,\"Cruz, Jr\",Swakopmund,Namibian,\"Line one\nLine two\"\n";
        var result = CandidateCsv.Parse(csv);

        result.Rows.Should().ContainSingle();
        result.Rows[0].LastName.Should().Be("Cruz, Jr");
        result.Rows[0].Headline.Should().Contain("Line one").And.Contain("Line two");
    }

    [Fact]
    public void Parse_reports_missing_required_header_and_missing_fields()
    {
        // No nationality column → whole-file error.
        CandidateCsv.Parse("firstName,lastName,city\nA,B,C\n").Errors.Should().NotBeEmpty();

        // Row missing a required field → row-level error, other rows still parse.
        var r = CandidateCsv.Parse("firstName,lastName,city,nationality\nJane,,Windhoek,Namibian\nJohn,Roe,Windhoek,Namibian\n");
        r.Rows.Should().ContainSingle(x => x.FirstName == "John");
        r.Errors.Should().Contain(e => e.Contains("Line 2"));
    }

    [Fact]
    public async Task Import_registers_new_and_skips_duplicates()
    {
        var repo = Substitute.For<ICandidateRepository>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        // One existing candidate that a CSV row duplicates (by name + city).
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { Candidate.Register("Jane", "Doe", "Windhoek", "Namibian").Value! });
        var handler = new ImportCandidatesCommandHandler(repo, publisher);

        // Row 1 duplicates the existing candidate; row 2 is new; row 3 duplicates row 2 within the batch.
        const string csv = "firstName,lastName,city,nationality\n" +
                           "Jane,Doe,Windhoek,Namibian\n" +
                           "John,Roe,Windhoek,Namibian\n" +
                           "John,Roe,Windhoek,Namibian\n";

        var result = await handler.HandleAsync(new ImportCandidatesCommand(csv), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Created.Should().Be(1);
        result.Value!.Skipped.Should().Be(2);
        repo.Received(1).Add(Arg.Any<Candidate>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Import_reports_invalid_availability_without_creating_that_row()
    {
        var repo = Substitute.For<ICandidateRepository>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        repo.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        var handler = new ImportCandidatesCommandHandler(repo, publisher);

        const string csv = "firstName,lastName,city,nationality,availability\n" +
                           "Ana,Vos,Windhoek,Namibian,Bogus\n";

        var result = await handler.HandleAsync(new ImportCandidatesCommand(csv), CancellationToken.None);

        result.Value!.Created.Should().Be(0);
        result.Value!.Errors.Should().Contain(e => e.Contains("Bogus"));
        repo.DidNotReceive().Add(Arg.Any<Candidate>());
    }
}
