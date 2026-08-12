using System.Net;
using System.Text;
using FluentAssertions;
using Illumin360.Matching;
using Xunit;

namespace Illumin360.Matching.UnitTests;

public class EmbeddingClientTests
{
    // A stub handler that records the request and returns a canned embeddings response.
    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }
    }

    [Fact]
    public void UseHosted_is_false_unless_provider_enabled_and_endpoint_set()
    {
        new EmbeddingOptions().UseHosted.Should().BeFalse();
        new EmbeddingOptions { Provider = EmbeddingProviderKind.Hosted }.UseHosted.Should().BeFalse();
        new EmbeddingOptions { Provider = EmbeddingProviderKind.Hosted, Enabled = true }.UseHosted.Should().BeFalse();
        new EmbeddingOptions { Provider = EmbeddingProviderKind.Hosted, Enabled = true, Endpoint = "https://api/embeddings" }.UseHosted.Should().BeTrue();
    }

    [Fact]
    public async Task Hosted_client_parses_and_normalizes_the_returned_vector()
    {
        var handler = new StubHandler("""{"data":[{"embedding":[3.0,4.0]}]}""");
        var client = new HostedEmbeddingClient(new HttpClient(handler), new EmbeddingOptions { Endpoint = "https://api/embeddings", Dimensions = 2 });

        var v = await client.EmbedAsync("hello world", CancellationToken.None);

        handler.Calls.Should().Be(1);
        // [3,4] normalised → [0.6,0.8].
        v[0].Should().BeApproximately(0.6f, 1e-4f);
        v[1].Should().BeApproximately(0.8f, 1e-4f);
    }

    [Fact]
    public async Task Hosted_client_short_circuits_blank_text_with_no_http_call()
    {
        var handler = new StubHandler("""{"data":[{"embedding":[1.0]}]}""");
        var client = new HostedEmbeddingClient(new HttpClient(handler), new EmbeddingOptions { Endpoint = "https://api/embeddings", Dimensions = 4 });

        var v = await client.EmbedAsync("   ", CancellationToken.None);

        handler.Calls.Should().Be(0);
        v.Should().HaveCount(4).And.OnlyContain(x => x == 0f);
    }

    [Fact]
    public async Task RankAsync_matches_the_synchronous_ranker_for_the_hashing_provider()
    {
        var provider = new HashingEmbeddingProvider(256);
        var pool = new (Guid, string?)[]
        {
            (Guid.Parse("00000000-0000-0000-0000-000000000001"), "senior react engineer typescript"),
            (Guid.Parse("00000000-0000-0000-0000-000000000002"), "registered nurse hospital"),
            (Guid.Parse("00000000-0000-0000-0000-000000000003"), "react frontend developer javascript"),
        };
        var seed = Guid.Empty;

        var sync = SemanticRanker.Rank(provider, "react developer", pool, seed, 3);
        var async = await SemanticRanker.RankAsync(provider, "react developer", pool, seed, 3);

        async.Select(m => (m.Id, m.Score)).Should().Equal(sync.Select(m => (m.Id, m.Score)));
    }
}
