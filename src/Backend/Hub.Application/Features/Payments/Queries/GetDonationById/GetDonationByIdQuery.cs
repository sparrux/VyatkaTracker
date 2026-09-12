namespace Hub.Application.Features.Payments.Queries.GetDonationById;

public sealed record GetDonationByIdQuery(
    Guid UserId,
    Guid DonationId
);
