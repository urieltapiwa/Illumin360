using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Illumin360.Email;
using Microsoft.Extensions.Options;
using Xunit;

namespace Illumin360.Email.IntegrationTests;

/// <summary>
/// Sends a real email through a Testcontainers Mailpit and verifies it landed via Mailpit's HTTP API.
/// Proves the SMTP sender works end-to-end. Requires a Docker daemon on the test host.
/// </summary>
public sealed class MailpitSendTests : IAsyncLifetime
{
    private readonly IContainer _mailpit = new ContainerBuilder()
        .WithImage("axllent/mailpit:latest")
        .WithPortBinding(1025, true)
        .WithPortBinding(8025, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8025))
        .Build();

    public Task InitializeAsync() => _mailpit.StartAsync();

    public Task DisposeAsync() => _mailpit.DisposeAsync().AsTask();

    [Fact]
    public async Task Sends_a_welcome_email_captured_by_mailpit()
    {
        var host = _mailpit.Hostname;
        var smtpPort = _mailpit.GetMappedPublicPort(1025);
        var apiPort = _mailpit.GetMappedPublicPort(8025);

        var sender = new MailKitEmailSender(Options.Create(new EmailOptions { Host = host, Port = smtpPort }));
        var email = NotificationTemplates.Welcome("Panduleni");
        await sender.SendAsync("candidate@illumin360.test", email.Subject, email.HtmlBody, CancellationToken.None);

        using var http = new HttpClient { BaseAddress = new Uri($"http://{host}:{apiPort}") };
        using var response = await http.GetAsync("/api/v1/messages");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        doc.RootElement.GetProperty("messages")[0].GetProperty("Subject").GetString()
            .Should().Be("Welcome to Illumin360");
    }
}
