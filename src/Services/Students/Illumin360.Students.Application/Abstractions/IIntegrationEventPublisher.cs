namespace Illumin360.Students.Application.Abstractions;

/// <summary>Port for publishing integration events to the message broker (via the transactional outbox).</summary>
public interface IIntegrationEventPublisher
{
    /// <summary>Publishes an integration event.</summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="integrationEvent">The event instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the event has been enqueued.</returns>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class;
}
