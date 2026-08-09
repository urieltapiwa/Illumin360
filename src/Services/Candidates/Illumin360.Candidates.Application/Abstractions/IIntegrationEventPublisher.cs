namespace Illumin360.Candidates.Application.Abstractions;

/// <summary>
/// Port for publishing integration events to other bounded contexts. Implemented by the
/// Infrastructure layer over the message broker's transactional outbox (charter Part 5).
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>Publishes an integration event. Delivery is transactional via the outbox.</summary>
    /// <typeparam name="TEvent">The integration event contract type.</typeparam>
    /// <param name="integrationEvent">The event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class;
}
