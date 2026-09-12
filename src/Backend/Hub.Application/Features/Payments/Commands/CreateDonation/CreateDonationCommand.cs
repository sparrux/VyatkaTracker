namespace Hub.Application.Features.Payments.Commands.CreateDonation;

public sealed record CreateDonationCommand(
    Guid UserId,
    CreateDonationRequest Request,
    string? IdempotencyKey
);

public sealed record CreateDonationRequest(
    decimal Amount,
    string Currency,
    bool IsAnonymous = false,
    Guid? EventId = null,
    string? Provider = null,
    string? Description = null,
    Uri? ReturnUrl = null,
    Uri? CancelUrl = null,
    string? IdempotencyKey = null
);
