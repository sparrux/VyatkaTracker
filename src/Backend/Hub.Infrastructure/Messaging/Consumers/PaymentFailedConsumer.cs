using Hub.Application.Features.Common.Contracts;
using Hub.Application.Features.Payments.Commands.HandlePaymentFailed;
using Hub.Application.Features.Payments.Messages;
using Hub.Application.Pipelines;
using MassTransit;

namespace Hub.Infrastructure.Messaging.Consumers;

sealed class PaymentFailedConsumer(
    IRequestHandler<HandlePaymentFailedCommand, IdResponse> handler
) : IConsumer<PaymentFailed>
{
    public Task Consume(ConsumeContext<PaymentFailed> context) =>
        RequestHandlerConsume.Consume(
            handler,
            new HandlePaymentFailedCommand(
                context.Message.PaymentId,
                context.Message.Purpose,
                context.Message.ReferenceId,
                context.Message.Reason),
            context.CancellationToken);
}
