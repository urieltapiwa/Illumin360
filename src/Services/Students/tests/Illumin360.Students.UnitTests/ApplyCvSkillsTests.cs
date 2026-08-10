using System.Text;
using FluentAssertions;
using Illumin360.Storage;
using Illumin360.Students.Application.Abstractions;
using Illumin360.Students.Application.Students;
using Illumin360.Students.Domain;
using NSubstitute;
using Xunit;

namespace Illumin360.Students.UnitTests;

public class ApplyCvSkillsTests
{
    [Fact]
    public async Task Adds_only_newly_detected_skills()
    {
        var me = Student.Register("Selma", "Nghidinwa", "Computer Science", "NUST", "Final year", "2026", "Illumin Futures", "Windhoek").Value!;
        me.SetCv("students/x/cv.txt", "cv.txt", "text/plain", 20, DateTimeOffset.UnixEpoch);

        var repo = Substitute.For<IStudentRepository>();
        repo.GetDefaultStudentIdAsync(Arg.Any<CancellationToken>()).Returns(me.Id);
        repo.GetTrackedAsync(me.Id, Arg.Any<CancellationToken>()).Returns(me);
        repo.GetSkillNamesAsync(me.Id, Arg.Any<CancellationToken>()).Returns(new[] { "Python" });

        var storage = Substitute.For<IObjectStorage>();
        storage.GetAsync(CvStorage.Bucket, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ObjectDownload(new MemoryStream(Encoding.UTF8.GetBytes("Python, SQL and Docker.")), "text/plain"));

        var handler = new ApplyCvSkillsCommandHandler(repo, storage);
        var result = await handler.HandleAsync(new ApplyCvSkillsCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Added.Should().BeEquivalentTo(["SQL", "Docker"]);
        repo.Received(2).AddSkill(Arg.Any<StudentSkill>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
