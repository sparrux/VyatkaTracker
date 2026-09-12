namespace Hub.Application.Features.Payments.Commands.HandlePaymentFailed;

public sealed record HandlePaymentFailedCommand(
    Guid PaymentId,
    string Purpose,
    Guid ReferenceId,
    string? Reason);
