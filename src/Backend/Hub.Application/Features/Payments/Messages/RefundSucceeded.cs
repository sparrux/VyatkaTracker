namespace Hub.Application.Features.Payments.Messages;

public sealed record RefundSucceeded(
    Guid EventId,
    Guid PaymentId,
    Guid RefundId,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredOn);
