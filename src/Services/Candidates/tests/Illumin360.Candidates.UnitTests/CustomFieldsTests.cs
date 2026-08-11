using FluentAssertions;
using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Application.Candidates;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;
using NSubstitute;
using Xunit;

namespace Illumin360.Candidates.UnitTests;

public class CustomFieldsTests
{
    [Fact]
    public void Definition_derives_key_and_validates_select_options()
    {
        CustomFieldDefinition.KeyFrom(" Right to Work? ").Should().Be("right-to-work");

        CustomFieldDefinition.Create("Notice period", "select", ["1 month"], 0, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();
        CustomFieldDefinition.Create("  ", "text", null, 0, DateTimeOffset.UnixEpoch).IsFailure.Should().BeTrue();

        var ok = CustomFieldDefinition.Create("Notice period", "select", ["Immediate", "1 month", "3 months"], 2, DateTimeOffset.UnixEpoch);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.Key.Should().Be("notice-period");
        ok.Value!.Options.Should().HaveCount(3);
    }

    [Fact]
    public async Task Add_rejects_duplicate_key()
    {
        var repo = Substitute.For<ICandidateRepository>();
        var existing = CustomFieldDefinition.Create("Right to work", "boolean", null, 0, DateTimeOffset.UnixEpoch).Value!;
        repo.ListCustomFieldsAsync(Arg.Any<CancellationToken>()).Returns(new[] { existing });
        var handler = new AddCustomFieldCommandHandler(repo);

        var dup = await handler.HandleAsync(new AddCustomFieldCommand("right to work", "text", null), CancellationToken.None);
        dup.IsFailure.Should().BeTrue();
        dup.Error!.Type.Should().Be(ErrorType.Conflict);

        var ok = await handler.HandleAsync(new AddCustomFieldCommand("Notice period", "text", null), CancellationToken.None);
        ok.IsSuccess.Should().BeTrue();
        ok.Value!.SortOrder.Should().Be(1); // appended after the existing field
    }

    [Fact]
    public async Task Set_values_replaces_prior_and_skips_blanks_and_unknown_fields()
    {
        var candidateId = Guid.NewGuid();
        var def = CustomFieldDefinition.Create("Notice period", "text", null, 0, DateTimeOffset.UnixEpoch).Value!;
        var repo = Substitute.For<ICandidateRepository>();
        repo.ListCustomFieldsAsync(Arg.Any<CancellationToken>()).Returns(new[] { def });
        var prior = CandidateCustomValue.Create(candidateId, def.Id, "old", DateTimeOffset.UnixEpoch).Value!;
        repo.ListCandidateValuesTrackedAsync(candidateId, Arg.Any<CancellationToken>()).Returns(new[] { prior });
        var handler = new SetCandidateCustomValuesCommandHandler(repo);

        var inputs = new List<CustomValueInput>
        {
            new(def.Id, "1 month"),          // valid
            new(Guid.NewGuid(), "orphan"),   // unknown field → skipped
            new(def.Id, "   "),              // blank → skipped
        };
        var result = await handler.HandleAsync(new SetCandidateCustomValuesCommand(candidateId, inputs), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        repo.Received(1).RemoveCandidateValue(prior);
        repo.Received(1).AddCandidateValue(Arg.Is<CandidateCustomValue>(v => v.Value == "1 month"));
    }

    [Fact]
    public async Task Get_values_returns_every_field_with_blank_when_unset()
    {
        var candidateId = Guid.NewGuid();
        var f1 = CustomFieldDefinition.Create("Right to work", "boolean", null, 0, DateTimeOffset.UnixEpoch).Value!;
        var f2 = CustomFieldDefinition.Create("Notice period", "text", null, 1, DateTimeOffset.UnixEpoch).Value!;
        var repo = Substitute.For<ICandidateRepository>();
        repo.ListCustomFieldsAsync(Arg.Any<CancellationToken>()).Returns(new[] { f1, f2 });
        repo.ListCandidateValuesAsync(candidateId, Arg.Any<CancellationToken>()).Returns(new[]
        {
            CandidateCustomValue.Create(candidateId, f1.Id, "true", DateTimeOffset.UnixEpoch).Value!,
        });
        var handler = new GetCandidateCustomValuesQueryHandler(repo);

        var result = await handler.HandleAsync(new GetCandidateCustomValuesQuery(candidateId), CancellationToken.None);

        result.Value!.Should().HaveCount(2);
        result.Value!.Single(v => v.Key == "right-to-work").Value.Should().Be("true");
        result.Value!.Single(v => v.Key == "notice-period").Value.Should().BeEmpty();
    }
}
