using FluentAssertions;
using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Application.Professionals;
using Illumin360.SharedKernel;
using Illumin360.Storage;
using NSubstitute;
using Xunit;

namespace Illumin360.Professionals.UnitTests;

public class ProfessionalCvTests
{
    [Fact]
    public void SetCv_RecordsMetadataAndFlagsHasCv()
    {
        var p = Domain.Professional.Register("Panduleni", "Amukwa", "Developer", "Windhoek", "Namibian", "Open", "Builder").Value!;

        var at = DateTimeOffset.UnixEpoch;
        p.SetCv("professionals/x/cv.pdf", "resume.pdf", "application/pdf", 1234, at);

        p.HasCv.Should().BeTrue();
        p.CvObjectKey.Should().Be("professionals/x/cv.pdf");
        p.CvFileName.Should().Be("resume.pdf");
        p.CvContentType.Should().Be("application/pdf");
        p.CvSize.Should().Be(1234);
        p.CvUploadedAt.Should().Be(at);
    }

    [Theory]
    [InlineData("image/png", 1000, "cv.unsupported_type")]
    [InlineData("application/pdf", 0, "cv.empty")]
    [InlineData("application/pdf", 6 * 1024 * 1024, "cv.too_large")]
    public async Task Upload_WithInvalidFile_FailsValidation(string contentType, long length, string expectedCode)
    {
        var repo = Substitute.For<IProfessionalRepository>();
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
