namespace Hub.Application.Features.Payments.Commands.ConfirmDonation;

public sealed record ConfirmDonationCommand(
    Guid UserId,
    Guid DonationId
);
