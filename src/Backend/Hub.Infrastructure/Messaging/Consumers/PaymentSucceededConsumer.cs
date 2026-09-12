using Hub.Application.Features.Common.Contracts;
using Hub.Application.Features.Payments.Commands.HandlePaymentSucceeded;
using Hub.Application.Features.Payments.Messages;
using Hub.Application.Pipelines;
using MassTransit;

namespace Hub.Infrastructure.Messaging.Consumers;

sealed class PaymentSucceededConsumer(
    IRequestHandler<HandlePaymentSucceededCommand, IdResponse> handler
) : IConsumer<PaymentSucceeded>
{
    public Task Consume(ConsumeContext<PaymentSucceeded> context) =>
        RequestHandlerConsume.Consume(
            handler,
            new HandlePaymentSucceededCommand(
                context.Message.PaymentId,
                context.Message.Purpose,
                context.Message.ReferenceId),
            context.CancellationToken);
}
