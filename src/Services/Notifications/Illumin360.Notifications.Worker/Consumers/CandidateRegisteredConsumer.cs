using Illumin360.Candidates.IntegrationEvents;
using Illumin360.Email;
using MassTransit;

namespace Illumin360.Notifications.Worker.Consumers;

/// <summary>
/// Consumes <see cref="CandidateRegistered"/> published by the Candidates service and sends a templated
/// welcome email via SMTP (Mailpit in dev). Exercises the full publish → broker → consume → email loop
/// fed by the Candidates transactional outbox (charter Part 5/13).
/// </summary>
/// <param name="emailSender">The SMTP email sender.</param>
/// <param name="logger">Logger.</param>
public sealed partial class CandidateRegisteredConsumer(IEmailSender emailSender, ILogger<CandidateRegisteredConsumer> logger)
    : IConsumer<CandidateRegistered>
{
    private readonly IEmailSender _emailSender = emailSender;
    private readonly ILogger<CandidateRegisteredConsumer> _logger = logger;

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<CandidateRegistered> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // The registration event carries only the id today, so we address the demo mailbox derived from it;
        // a production build resolves the real address from the candidate's profile.
        var toAddress = $"candidate+{context.Message.CandidateId}@illumin360.test";
        var email = NotificationTemplates.Welcome(name: null);
        await _emailSender.SendAsync(toAddress, email.Subject, email.HtmlBody, context.CancellationToken).ConfigureAwait(false);

        LogWelcomeSent(context.Message.CandidateId, context.Message.OccurredOn);
    }

    // Source-generated, allocation-free structured logging (satisfies CA1848/CA1873).
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Welcome email sent for candidate {CandidateId} (registered {OccurredOn}).")]
    private partial void LogWelcomeSent(Guid candidateId, DateTimeOffset occurredOn);
}
