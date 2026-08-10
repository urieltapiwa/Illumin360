using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class JobTemplatesTests
{
    [Fact]
    public void Create_validates_and_normalizes_tags()
    {
        JobTemplate.Create("", "Dev", null, 1, null, null, null, null, false, null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        JobTemplate.Create("T", "Dev", null, 0, null, null, null, null, false, null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        JobTemplate.Create("T", "Dev", null, 1, 90, 10, null, null, false, null, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();

        var ok = JobTemplate.Create("Backend role", "Software Developer", "Windhoek", 2, 40000, 60000, "nad", "contract", true, new[] { "Backend", "backend", "URGENT" }, DateTimeOffset.UnixEpoch);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.Currency.Should().Be("NAD");
        ok.Value!.EmploymentType.Should().Be(EmploymentType.Contract);
        ok.Value!.Tags.Should().BeEquivalentTo(["backend", "urgent"]);
    }

    [Fact]
    public async Task Create_conflicts_on_duplicate_name()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.JobTemplateNameExistsAsync("Backend role", Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateJobTemplateCommandHandler(repo);

        var result = await handler.HandleAsync(new CreateJobTemplateCommand("Backend role", "Dev", null, 1, null, null, null, null, false, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        repo.DidNotReceive().AddJobTemplate(Arg.Any<JobTemplate>());
    }

    [Fact]
    public async Task Create_persists_template()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.JobTemplateNameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateJobTemplateCommandHandler(repo);

        var result = await handler.HandleAsync(new CreateJobTemplateCommand("Ops role", "Operations Lead", "Walvis Bay", 1, null, null, "NAD", "fulltime", false, ["ops"]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Ops role");
        repo.Received(1).AddJobTemplate(Arg.Any<JobTemplate>());
    }

    [Fact]
    public async Task Use_missing_template_is_not_found()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetJobTemplateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((JobTemplate?)null);
        var handler = new UseJobTemplateCommandHandler(repo);

        var result = await handler.HandleAsync(new UseJobTemplateCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Use_creates_request_with_detail_and_tags()
    {
        var template = JobTemplate.Create("Backend role", "Software Developer", "Windhoek", 3, 40000, 60000, "NAD", "contract", true, ["backend", "urgent"], DateTimeOffset.UnixEpoch).Value!;
        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetJobTemplateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(template);
        var handler = new UseJobTemplateCommandHandler(repo);

        var result = await handler.HandleAsync(new UseJobTemplateCommand(template.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Software Developer");
        result.Value!.Positions.Should().Be(3);
        repo.Received(1).Add(Arg.Any<RecruitmentRequest>());
        repo.Received(1).AddRequisitionDetail(Arg.Any<RequisitionDetail>());
        repo.Received(2).AddRequisitionTag(Arg.Any<RequisitionTag>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
