using Ardalis.Result;
using Hub.Application.Features.Common.Contracts;
using Hub.Application.Pipelines;
using Microsoft.Extensions.Logging;

namespace Hub.Application.Features.Payments.Commands.HandleRefundSucceeded;

sealed class HandleRefundSucceededCommandHandler(
    ILogger<HandleRefundSucceededCommandHandler> logger
) : IRequestHandler<HandleRefundSucceededCommand, IdResponse>
{
    public Task<Result<IdResponse>> Handle(
        HandleRefundSucceededCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Refund {RefundId} of {Amount} {Currency} succeeded for payment {PaymentId}",
            command.RefundId,
            command.Amount,
            command.Currency,
            command.PaymentId);

        return Task.FromResult(Result.Success(new IdResponse(command.RefundId)));
    }
}
