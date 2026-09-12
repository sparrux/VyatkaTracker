using Hub.Application.Abstractions.Messaging;
using MassTransit;

namespace Hub.Infrastructure.Messaging;

sealed class MassTransitIntegrationEventPublisher(
    IPublishEndpoint publishEndpoint
) : IIntegrationEventPublisher
{
    public Task Publish(object message, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(message, message.GetType(), cancellationToken);
}
