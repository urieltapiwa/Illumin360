using System.Text;
using FluentAssertions;
using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Application.Professionals;
using Illumin360.Professionals.Domain;
using Illumin360.Storage;
using NSubstitute;
using Xunit;

namespace Illumin360.Professionals.UnitTests;

public class ApplyCvSkillsTests
{
    [Fact]
    public async Task Adds_only_newly_detected_skills()
    {
        var me = Professional.Register("Panduleni", "Amukwa", "Developer", "Windhoek", "Namibian", "Open", "Builder").Value!;
        me.SetCv("professionals/x/cv.txt", "cv.txt", "text/plain", 20, DateTimeOffset.UnixEpoch);

        var repo = Substitute.For<IProfessionalRepository>();
        repo.GetDefaultProfessionalIdAsync(Arg.Any<CancellationToken>()).Returns(me.Id);
        repo.GetTrackedAsync(me.Id, Arg.Any<CancellationToken>()).Returns(me);
        repo.GetSkillNamesAsync(me.Id, Arg.Any<CancellationToken>()).Returns(new[] { "Python" });

        var storage = Substitute.For<IObjectStorage>();
        storage.GetAsync(CvStorage.Bucket, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ObjectDownload(new MemoryStream(Encoding.UTF8.GetBytes("Python, SQL and Docker.")), "text/plain"));

        var handler = new ApplyCvSkillsCommandHandler(repo, storage);
        var result = await handler.HandleAsync(new ApplyCvSkillsCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Detected.Should().Contain(["Python", "SQL", "Docker"]);
        result.Value!.Added.Should().BeEquivalentTo(["SQL", "Docker"]); // Python already present
        repo.Received(2).AddSkill(Arg.Any<ProfessionalSkill>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NoCv_returns_not_found()
    {
        var me = Professional.Register("Panduleni", "Amukwa", "Developer", "Windhoek", "Namibian", "Open", "Builder").Value!;
        var repo = Substitute.For<IProfessionalRepository>();
        repo.GetDefaultProfessionalIdAsync(Arg.Any<CancellationToken>()).Returns(me.Id);
        repo.GetTrackedAsync(me.Id, Arg.Any<CancellationToken>()).Returns(me);
        var handler = new ApplyCvSkillsCommandHandler(repo, Substitute.For<IObjectStorage>());

        var result = await handler.HandleAsync(new ApplyCvSkillsCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
