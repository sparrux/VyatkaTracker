namespace Hub.Application.Features.Payments.Commands.HandlePaymentSucceeded;

public sealed record HandlePaymentSucceededCommand(
    Guid PaymentId,
    string Purpose,
    Guid ReferenceId);
