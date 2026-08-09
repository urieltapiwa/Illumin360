using Illumin360.Candidates.IntegrationEvents;
using MassTransit;

namespace Illumin360.Notifications.Worker.Consumers;

/// <summary>
/// Consumes <see cref="CandidateRegistered"/> published by the Candidates service and triggers
/// onboarding notifications (e.g. a welcome email). Exercises the full publish → broker → consume
/// loop fed by the Candidates transactional outbox (charter Part 5/13).
/// </summary>
public sealed partial class CandidateRegisteredConsumer(ILogger<CandidateRegisteredConsumer> logger)
    : IConsumer<CandidateRegistered>
{
    private readonly ILogger<CandidateRegisteredConsumer> _logger = logger;

    /// <inheritdoc />
    public Task Consume(ConsumeContext<CandidateRegistered> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Placeholder side effect — a real implementation would enqueue an onboarding email /
        // push the candidate into the matching pipeline. Logged so the loop is observable end-to-end.
        LogOnboardingQueued(context.Message.CandidateId, context.Message.OccurredOn);

        return Task.CompletedTask;
    }

    // Source-generated, allocation-free structured logging (satisfies CA1848/CA1873).
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Onboarding notification queued for candidate {CandidateId} (registered {OccurredOn}).")]
    private partial void LogOnboardingQueued(Guid candidateId, DateTimeOffset occurredOn);
}
