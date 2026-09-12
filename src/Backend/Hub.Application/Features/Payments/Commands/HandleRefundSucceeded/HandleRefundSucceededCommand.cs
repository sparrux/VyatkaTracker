namespace Hub.Application.Features.Payments.Commands.HandleRefundSucceeded;

public sealed record HandleRefundSucceededCommand(
    Guid PaymentId,
    Guid RefundId,
    decimal Amount,
    string Currency);
