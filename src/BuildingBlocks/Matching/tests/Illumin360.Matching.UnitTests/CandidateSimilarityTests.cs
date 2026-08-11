using FluentAssertions;
using Illumin360.Matching;
using Xunit;

namespace Illumin360.Matching.UnitTests;

public class CandidateSimilarityTests
{
    private static readonly Guid Seed = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid C = Guid.Parse("00000000-0000-0000-0000-00000000000c");

    [Fact]
    public void Ranks_by_shared_city_availability_and_headline_and_excludes_seed()
    {
        var seed = new CandidateFeatures("Windhoek", "ActivelyLooking", "Backend software developer");
        var pool = new (Guid, CandidateFeatures)[]
        {
            (Seed, seed), // must be excluded
            (A, new CandidateFeatures("Windhoek", "ActivelyLooking", "Backend software developer")), // near-identical
            (B, new CandidateFeatures("Windhoek", "NotAvailable", "Frontend designer")),              // same city only
            (C, new CandidateFeatures("Walvis Bay", "NotAvailable", "Chef")),                          // nothing shared
        };

        var result = CandidateSimilarity.Rank(seed, pool, Seed, 10);

        result.Should().NotContain(m => m.Id == Seed);
        result[0].Id.Should().Be(A);
        result[0].Score.Should().BeGreaterThan(result[^1].Score);
        result.Should().NotContain(m => m.Id == C); // zero-similarity dropped
    }

    [Fact]
    public void Respects_take_and_returns_empty_for_nonpositive_take()
    {
        var seed = new CandidateFeatures("Windhoek", "ActivelyLooking", "developer");
        var pool = new (Guid, CandidateFeatures)[]
        {
            (A, new CandidateFeatures("Windhoek", "ActivelyLooking", "developer")),
            (B, new CandidateFeatures("Windhoek", "ActivelyLooking", "developer")),
        };

        CandidateSimilarity.Rank(seed, pool, Seed, 1).Should().ContainSingle();
        CandidateSimilarity.Rank(seed, pool, Seed, 0).Should().BeEmpty();
    }
}
