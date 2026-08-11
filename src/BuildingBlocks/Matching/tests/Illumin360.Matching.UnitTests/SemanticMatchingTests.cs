using FluentAssertions;
using Illumin360.Matching;
using Xunit;

namespace Illumin360.Matching.UnitTests;

public class SemanticMatchingTests
{
    private static readonly Guid Seed = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-00000000000b");

    [Fact]
    public void Hashing_embeddings_are_deterministic_and_unit_length()
    {
        var provider = new HashingEmbeddingProvider(128);
        var v1 = provider.Embed("Senior backend engineer");
        var v2 = provider.Embed("Senior backend engineer");

        v1.Should().Equal(v2); // deterministic across calls (FNV-1a, not randomised hashing)
        v1.Should().HaveCount(128);
        VectorMath.Cosine(v1, v2).Should().BeApproximately(1.0, 1e-6);
    }

    [Fact]
    public void Blank_text_is_a_zero_vector_and_cosine_is_zero()
    {
        var provider = new HashingEmbeddingProvider(64);
        var zero = provider.Embed("   ");
        zero.Should().OnlyContain(x => x == 0f);
        VectorMath.Cosine(zero, provider.Embed("developer")).Should().Be(0);
    }

    [Fact]
    public void Cosine_rewards_shared_terms()
    {
        var provider = new HashingEmbeddingProvider(512);
        var a = provider.Embed("backend software engineer golang postgres");
        var near = provider.Embed("backend engineer golang");
        var far = provider.Embed("pastry chef bakery");

        VectorMath.Cosine(a, near).Should().BeGreaterThan(VectorMath.Cosine(a, far));
    }

    [Fact]
    public void Ranker_returns_closest_first_and_excludes_seed()
    {
        var provider = new HashingEmbeddingProvider(512);
        var pool = new (Guid, string?)[]
        {
            (Seed, "backend engineer golang"),         // excluded
            (A, "senior backend engineer golang api"),  // close
            (B, "graphic designer illustrator"),         // unrelated
        };

        var result = SemanticRanker.Rank(provider, "backend engineer golang", pool, Seed, 10);

        result.Should().NotContain(m => m.Id == Seed);
        result[0].Id.Should().Be(A);
        result.Should().NotContain(m => m.Id == B); // below minScore, dropped
    }

    [Fact]
    public void Ranker_empty_for_blank_query_or_nonpositive_take()
    {
        var provider = new HashingEmbeddingProvider();
        var pool = new (Guid, string?)[] { (A, "developer") };
        SemanticRanker.Rank(provider, "", pool, Seed, 5).Should().BeEmpty();
        SemanticRanker.Rank(provider, "developer", pool, Seed, 0).Should().BeEmpty();
    }
}
