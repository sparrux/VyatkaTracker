using Hub.Application.Features.Common.Contracts;
using Hub.Application.Features.Payments.Commands.HandleRefundSucceeded;
using Hub.Application.Features.Payments.Messages;
using Hub.Application.Pipelines;
using MassTransit;

namespace Hub.Infrastructure.Messaging.Consumers;

sealed class RefundSucceededConsumer(
    IRequestHandler<HandleRefundSucceededCommand, IdResponse> handler
) : IConsumer<RefundSucceeded>
{
    public Task Consume(ConsumeContext<RefundSucceeded> context) =>
        RequestHandlerConsume.Consume(
            handler,
            new HandleRefundSucceededCommand(
                context.Message.PaymentId,
                context.Message.RefundId,
                context.Message.Amount,
                context.Message.Currency),
            context.CancellationToken);
}
