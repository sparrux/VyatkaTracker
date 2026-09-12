using Ardalis.Result;
using Hub.Application.Abstractions;
using Hub.Application.Features.Payments.Contracts;
using Hub.Application.Pipelines;
using Microsoft.EntityFrameworkCore;

namespace Hub.Application.Features.Payments.Queries.GetDonationById;

sealed class GetDonationByIdQueryHandler(
    IPaymentsDbContext paymentsDbContext
) : IRequestHandler<GetDonationByIdQuery, DonationResponse>
{
    public async Task<Result<DonationResponse>> Handle(
        GetDonationByIdQuery query,
        CancellationToken cancellationToken)
    {
        var donation = await paymentsDbContext.Donations
            .FirstOrDefaultAsync(
                x => x.Id == query.DonationId && x.CustomerId == query.UserId,
                cancellationToken);

        if (donation is null)
            return Result.NotFound("Donation not found");

        var payment = donation.PaymentId is null
            ? null
            : await paymentsDbContext.Payments
                .Include(x => x.Attempts)
                .FirstOrDefaultAsync(x => x.Id == donation.PaymentId, cancellationToken);

        return Result.Success(DonationResponse.From(donation, payment));
    }
}
