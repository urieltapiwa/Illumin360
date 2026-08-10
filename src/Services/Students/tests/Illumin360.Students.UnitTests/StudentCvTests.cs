using FluentAssertions;
using Illumin360.SharedKernel;
using Illumin360.Storage;
using Illumin360.Students.Application.Abstractions;
using Illumin360.Students.Application.Students;
using NSubstitute;
using Xunit;

namespace Illumin360.Students.UnitTests;

public class StudentCvTests
{
    [Fact]
    public void SetCv_RecordsMetadataAndFlagsHasCv()
    {
        var s = Domain.Student.Register("Selma", "Nghidinwa", "Computer Science", "NUST", "Final year", "2026", "Illumin Futures", "Windhoek").Value!;

        var at = DateTimeOffset.UnixEpoch;
        s.SetCv("students/x/cv.pdf", "resume.pdf", "application/pdf", 999, at);

        s.HasCv.Should().BeTrue();
        s.CvFileName.Should().Be("resume.pdf");
        s.CvContentType.Should().Be("application/pdf");
        s.CvSize.Should().Be(999);
        s.CvUploadedAt.Should().Be(at);
    }

    [Theory]
    [InlineData("image/png", 1000, "cv.unsupported_type")]
    [InlineData("application/pdf", 0, "cv.empty")]
    [InlineData("application/pdf", 6 * 1024 * 1024, "cv.too_large")]
    public async Task Upload_WithInvalidFile_FailsValidation(string contentType, long length, string expectedCode)
    {
        var repo = Substitute.For<IStudentRepository>();
        var storage = Substitute.For<IObjectStorage>();
        var handler = new UploadCvCommandHandler(repo, storage);

        var result = await handler.HandleAsync(
            new UploadCvCommand("cv", contentType, length, Stream.Null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error!.Code.Should().Be(expectedCode);
        await storage.DidNotReceiveWithAnyArgs().PutAsync(default!, default!, default!, default!, default);
    }
}
