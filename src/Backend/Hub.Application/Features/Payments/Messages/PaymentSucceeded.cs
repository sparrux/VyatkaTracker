namespace Hub.Application.Features.Payments.Messages;

public sealed record PaymentSucceeded(
    Guid EventId,
    Guid PaymentId,
    Guid? CustomerId,
    string Purpose,
    Guid ReferenceId,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredOn);
