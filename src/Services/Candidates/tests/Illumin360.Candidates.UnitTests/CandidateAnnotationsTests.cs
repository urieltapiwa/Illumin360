using FluentAssertions;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class CandidateAnnotationsTests
{
    private static readonly CandidateId Cid = CandidateId.New();

    private static Candidate ACandidate()
        => Candidate.Register("Ada", "Lovelace", "Windhoek", "Namibian").Value!;

    [Fact]
    public void Note_requires_body_and_defaults_author()
    {
        CandidateNote.Create(Cid, null, "  ", DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        var ok = CandidateNote.Create(Cid, null, "Strong interview", DateTimeOffset.UnixEpoch);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.Author.Should().Be("Recruiter");
    }

    [Fact]
    public void Tag_normalizes_label()
    {
        var ok = CandidateTag.Create(Cid, "  Backend  ", DateTimeOffset.UnixEpoch);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.Label.Should().Be("backend");
        CandidateTag.Create(Cid, "", DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Add_note_to_missing_candidate_is_not_found()
    {
        var repo = Substitute.For<ICandidateRepository>();
        repo.GetByIdAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns((Candidate?)null);
        var handler = new AddCandidateNoteCommandHandler(repo);

        var result = await handler.HandleAsync(new AddCandidateNoteCommand(Guid.NewGuid(), "Rec", "Hi"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        repo.DidNotReceive().AddNote(Arg.Any<CandidateNote>());
    }

    [Fact]
    public async Task Add_note_persists()
    {
        var repo = Substitute.For<ICandidateRepository>();
        repo.GetByIdAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns(ACandidate());
        var handler = new AddCandidateNoteCommandHandler(repo);

        var result = await handler.HandleAsync(new AddCandidateNoteCommand(Guid.NewGuid(), "Rita", "Great fit"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Body.Should().Be("Great fit");
        repo.Received(1).AddNote(Arg.Any<CandidateNote>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_tag_is_idempotent()
    {
        var repo = Substitute.For<ICandidateRepository>();
        repo.GetByIdAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns(ACandidate());
        repo.TagExistsAsync(Arg.Any<CandidateId>(), "backend", Arg.Any<CancellationToken>()).Returns(true);
        repo.ListTagsAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>())
            .Returns(new[] { CandidateTag.Create(Cid, "backend", DateTimeOffset.UnixEpoch).Value! });
        var handler = new AddCandidateTagCommandHandler(repo);

        var result = await handler.HandleAsync(new AddCandidateTagCommand(Guid.NewGuid(), "Backend"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle().Which.Should().Be("backend");
        repo.DidNotReceive().AddTag(Arg.Any<CandidateTag>());
    }

    [Fact]
    public async Task Add_new_tag_persists_and_returns_labels()
    {
        var repo = Substitute.For<ICandidateRepository>();
        repo.GetByIdAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns(ACandidate());
        repo.TagExistsAsync(Arg.Any<CandidateId>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        repo.ListTagsAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>())
            .Returns(new[] { CandidateTag.Create(Cid, "senior", DateTimeOffset.UnixEpoch).Value! });
        var handler = new AddCandidateTagCommandHandler(repo);

        var result = await handler.HandleAsync(new AddCandidateTagCommand(Guid.NewGuid(), "Senior"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repo.Received(1).AddTag(Arg.Any<CandidateTag>());
    }
}
