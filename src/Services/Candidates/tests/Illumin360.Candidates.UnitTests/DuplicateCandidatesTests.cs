using FluentAssertions;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;
using NSubstitute;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class DuplicateCandidatesTests
{
    private static Candidate C(string first, string last, string city)
        => Candidate.Register(first, last, city, "Namibian").Value!;

    [Fact]
    public async Task Groups_candidates_sharing_a_name_case_insensitively()
    {
        var repo = Substitute.For<ICandidateRepository>();
        repo.ListAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new[]
        {
            C("Maria", "Nangolo", "Windhoek"),
            C("maria", " nangolo ", "Swakopmund"),
            C("John", "Smith", "Windhoek"),
        });
        var handler = new FindDuplicateCandidatesQueryHandler(repo);

        var result = await handler.HandleAsync(new FindDuplicateCandidatesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle();
        result.Value![0].Count.Should().Be(2);
        result.Value![0].Name.Should().Be("Maria Nangolo");
    }

    [Fact]
    public async Task Same_city_only_splits_by_city()
    {
        var repo = Substitute.For<ICandidateRepository>();
        repo.ListAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new[]
        {
            C("Maria", "Nangolo", "Windhoek"),
            C("Maria", "Nangolo", "Swakopmund"),
        });
        var handler = new FindDuplicateCandidatesQueryHandler(repo);

        var strict = await handler.HandleAsync(new FindDuplicateCandidatesQuery(SameCityOnly: true), CancellationToken.None);
        strict.Value!.Should().BeEmpty(); // different cities → not a duplicate under strict mode

        var loose = await handler.HandleAsync(new FindDuplicateCandidatesQuery(SameCityOnly: false), CancellationToken.None);
        loose.Value!.Should().ContainSingle();
    }

    [Fact]
    public async Task No_duplicates_returns_empty()
    {
        var repo = Substitute.For<ICandidateRepository>();
        repo.ListAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new[]
        {
            C("Ada", "Lovelace", "Windhoek"),
            C("Grace", "Hopper", "Windhoek"),
        });
        var handler = new FindDuplicateCandidatesQueryHandler(repo);

        var result = await handler.HandleAsync(new FindDuplicateCandidatesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEmpty();
    }
}
