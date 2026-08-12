using System.Net;
using System.Text;
using FluentAssertions;
using Illumin360.Ai;
using Illumin360.Recruitment.Application.Recruitment;
using Xunit;

namespace Illumin360.Recruitment.UnitTests;

public class WritingAssistantTests
{
    private sealed class StubHandler(string content) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var quoted = System.Text.Json.JsonSerializer.Serialize(content);
            var json = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":" + quoted + "}}]}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public void AiOptions_UseHosted_is_gated()
    {
        new AiOptions().UseHosted.Should().BeFalse();
        new AiOptions { Provider = AiProviderKind.Hosted, Enabled = true, Endpoint = "https://api/chat" }.UseHosted.Should().BeTrue();
    }

    [Fact]
    public void Templates_produce_sensible_offline_output()
    {
        AiTemplates.JobDescription("Senior Engineer", "Windhoek", ["C#", "SQL"]).Should().Contain("Senior Engineer").And.Contain("C#");
        AiTemplates.Summarize("First sentence. Second sentence. Third one here.", 2).Should().StartWith("First sentence");
        AiTemplates.DraftMessage("Aria for the SWE role", "invite to interview").Should().Contain("Aria for the SWE role");
    }

    [Fact]
    public async Task Job_description_uses_the_local_template_when_ai_is_disabled()
    {
        var handler = new GenerateJobDescriptionCommandHandler(new DisabledTextCompletionClient());

        var result = await handler.HandleAsync(new GenerateJobDescriptionCommand("Data Analyst", "Windhoek", ["SQL"]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Source.Should().Be("template");
        result.Value!.Text.Should().Contain("Data Analyst");
    }

    [Fact]
    public async Task Job_description_uses_the_hosted_model_when_enabled()
    {
        var handler = new StubHandler("A crafted JD from the model.");
        var client = new HostedTextCompletionClient(new HttpClient(handler), new AiOptions { Endpoint = "https://api/chat", Enabled = true, Provider = AiProviderKind.Hosted });
        var sut = new GenerateJobDescriptionCommandHandler(client);

        var result = await sut.HandleAsync(new GenerateJobDescriptionCommand("Data Analyst", "Windhoek", ["SQL"]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Source.Should().Be("hosted");
        result.Value!.Text.Should().Be("A crafted JD from the model.");
        handler.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Blank_title_is_rejected()
    {
        var handler = new GenerateJobDescriptionCommandHandler(new DisabledTextCompletionClient());
        var result = await handler.HandleAsync(new GenerateJobDescriptionCommand("   ", null, null), CancellationToken.None);
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("assistant.title_required");
    }
}
