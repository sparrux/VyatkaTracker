namespace Hub.Application.Features.Payments.Commands.RecordCashDonation;

public sealed record RecordCashDonationCommand(
    RecordCashDonationRequest Request,
    string? IdempotencyKey
);

public sealed record RecordCashDonationRequest(
    Guid UserId,
    decimal Amount,
    string Currency,
    bool IsAnonymous = false,
    Guid? EventId = null,
    string? Description = null,
    string? IdempotencyKey = null
);
