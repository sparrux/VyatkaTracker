using Hub.Application.Features.Common.Contracts;
using Hub.Application.Features.Payments.Commands.ProcessPaymentWebhook;
using Hub.Application.Features.Payments.Messages;
using Hub.Application.Pipelines;
using MassTransit;

namespace Hub.Infrastructure.Messaging.Consumers;

sealed class ProcessPaymentWebhookConsumer(
    IRequestHandler<ProcessPaymentWebhookCommand, IdResponse> handler
) : IConsumer<ProcessPaymentWebhook>
{
    public Task Consume(ConsumeContext<ProcessPaymentWebhook> context) =>
        RequestHandlerConsume.Consume(
            handler,
            new ProcessPaymentWebhookCommand(context.Message.WebhookEventId),
            context.CancellationToken);
}
