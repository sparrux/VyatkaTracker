namespace Hub.Application.Abstractions.Messaging;

public interface IIntegrationEventPublisher
{
    Task Publish(object message, CancellationToken cancellationToken);
}
