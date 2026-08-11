using FluentAssertions;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class CandidateExportTests
{
    [Fact]
    public async Task Missing_candidate_is_not_found()
    {
        var repo = Substitute.For<ICandidateRepository>();
        repo.GetByIdAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns((Candidate?)null);
        var handler = new GetCandidateExportQueryHandler(repo);

        var result = await handler.HandleAsync(new GetCandidateExportQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Assembles_profile_notes_and_tags()
    {
        var candidate = Candidate.Register("Ada", "Lovelace", "Windhoek", "Namibian", AvailabilityStatus.OpenToOpportunities, "Backend engineer").Value!;
        var note = CandidateNote.Create(candidate.Id, "Rita", "Strong interview.", DateTimeOffset.UnixEpoch).Value!;
        var tag = CandidateTag.Create(candidate.Id, "backend", DateTimeOffset.UnixEpoch).Value!;
        var repo = Substitute.For<ICandidateRepository>();
        repo.GetByIdAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns(candidate);
        repo.ListNotesAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns(new[] { note });
        repo.ListTagsAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns(new[] { tag });
        var handler = new GetCandidateExportQueryHandler(repo);

        var result = await handler.HandleAsync(new GetCandidateExportQuery(candidate.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var export = result.Value!;
        export.FirstName.Should().Be("Ada");
        export.Availability.Should().Be("OpenToOpportunities");
        export.Cv.HasCv.Should().BeFalse();
        export.Notes.Should().ContainSingle(n => n.Body == "Strong interview." && n.Author == "Rita");
        export.Tags.Should().ContainSingle().Which.Should().Be("backend");
        export.GeneratedAt.Should().BeAfter(DateTimeOffset.UnixEpoch);
    }
}
