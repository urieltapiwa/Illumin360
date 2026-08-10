using FluentAssertions;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Application.Recruitment;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class SavedSearchesTests
{
    [Fact]
    public async Task Create_persists_and_returns_the_saved_search()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        var handler = new CreateSavedSearchCommandHandler(repo);

        var result = await handler.HandleAsync(
            new CreateSavedSearchCommand(Guid.NewGuid(), "Dev roles in Windhoek", "Windhoek", "developer", true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Label.Should().Be("Dev roles in Windhoek");
        result.Value!.AlertsEnabled.Should().BeTrue();
        repo.Received(1).AddSavedSearch(Arg.Any<SavedSearch>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_without_label_fails_validation()
    {
        var repo = Substitute.For<IRecruitmentRepository>();
        var handler = new CreateSavedSearchCommandHandler(repo);

        var result = await handler.HandleAsync(
            new CreateSavedSearchCommand(Guid.NewGuid(), "  ", null, null, false), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Run_filters_open_roles_by_keyword()
    {
        var search = SavedSearch.Create(Guid.NewGuid(), "Dev", null, "developer", false, DateTimeOffset.UnixEpoch).Value!;
        var dev = RecruitmentRequest.Post(Guid.NewGuid(), "Software Developer", "Windhoek", 1).Value!;
        var chef = RecruitmentRequest.Post(Guid.NewGuid(), "Head Chef", "Windhoek", 1).Value!;

        var repo = Substitute.For<IRecruitmentRepository>();
        repo.GetSavedSearchAsync(Arg.Any<SavedSearchId>(), Arg.Any<CancellationToken>()).Returns(search);
        repo.ListAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { dev, chef });

        var handler = new RunSavedSearchQueryHandler(repo);
        var result = await handler.HandleAsync(new RunSavedSearchQuery(search.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle();
        result.Value![0].Title.Should().Be("Software Developer");
    }
}
