using Illumin360.Admin.Application.Abstractions;
using MassTransit;

namespace Illumin360.Admin.Infrastructure.Messaging;

/// <summary>
/// Publishes integration events via MassTransit. When the EF Core bus outbox is enabled,
/// <c>IPublishEndpoint.Publish</c> writes the message to the outbox tables inside the
/// current DbContext transaction; it is delivered to the broker only after commit.
/// </summary>
public sealed class MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class
        => _publishEndpoint.Publish(integrationEvent, cancellationToken);
}
