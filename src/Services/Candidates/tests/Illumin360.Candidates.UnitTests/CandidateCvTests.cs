using FluentAssertions;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;
using Illumin360.Storage;
using NSubstitute;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class CandidateCvTests
{
    [Fact]
    public void SetCv_RecordsMetadataAndFlagsHasCv()
    {
        var c = Candidate.Register("Tariro", "Moyo", "Windhoek", "Namibian").Value!;

        var at = DateTimeOffset.UnixEpoch;
        c.SetCv("candidates/x/cv.pdf", "resume.pdf", "application/pdf", 512, at);

        c.HasCv.Should().BeTrue();
        c.CvFileName.Should().Be("resume.pdf");
        c.CvContentType.Should().Be("application/pdf");
        c.CvSize.Should().Be(512);
        c.CvUploadedAt.Should().Be(at);
    }

    [Theory]
    [InlineData("image/png", 1000, "cv.unsupported_type")]
    [InlineData("application/pdf", 0, "cv.empty")]
    [InlineData("application/pdf", 6 * 1024 * 1024, "cv.too_large")]
    public async Task Upload_WithInvalidFile_FailsValidation(string contentType, long length, string expectedCode)
    {
        var repo = Substitute.For<ICandidateRepository>();
        var storage = Substitute.For<IObjectStorage>();
        var handler = new UploadCandidateCvCommandHandler(repo, storage);

        var result = await handler.HandleAsync(
            new UploadCandidateCvCommand(Guid.NewGuid(), "cv", contentType, length, Stream.Null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error!.Code.Should().Be(expectedCode);
        await storage.DidNotReceiveWithAnyArgs().PutAsync(default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task Upload_ForUnknownCandidate_ReturnsNotFound()
    {
        var repo = Substitute.For<ICandidateRepository>();
        repo.GetByIdAsync(Arg.Any<CandidateId>(), Arg.Any<CancellationToken>()).Returns((Candidate?)null);
        var storage = Substitute.For<IObjectStorage>();
        var handler = new UploadCandidateCvCommandHandler(repo, storage);

        var result = await handler.HandleAsync(
            new UploadCandidateCvCommand(Guid.NewGuid(), "cv.pdf", "application/pdf", 1000, Stream.Null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}
