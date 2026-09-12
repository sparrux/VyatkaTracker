namespace Hub.Application.Features.Payments.Messages;

public sealed record PaymentFailed(
    Guid EventId,
    Guid PaymentId,
    string Purpose,
    Guid ReferenceId,
    string? Reason,
    DateTimeOffset OccurredOn);
