using Ardalis.Result;
using Hub.Application.Abstractions;
using Hub.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace Hub.Application.Features.Payments;

static class CashDonationAccess
{
    public static async Task<Result<(Donation Donation, Payment Payment, PaymentAttempt Attempt)>> LoadAsync(
        IPaymentsDbContext paymentsDbContext,
        Guid donationId,
        CancellationToken cancellationToken)
    {
        var donation = await paymentsDbContext.Donations
            .FirstOrDefaultAsync(x => x.Id == donationId, cancellationToken);

        if (donation is null)
            return Result.NotFound("Donation not found");

        if (donation.PaymentId is null)
            return Result.Error("Donation has no payment attached");

        var payment = await paymentsDbContext.Payments
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.Id == donation.PaymentId, cancellationToken);

        if (payment is null)
            return Result.Error("Payment not found for donation");

        var attempt = payment.Attempts
            .OrderByDescending(x => x.AttemptNumber)
            .FirstOrDefault();

        if (attempt is null)
            return Result.Error("Donation payment has not been started");

        if (!DonationCheckout.IsCash(attempt))
            return Result.Error("Only cash payments can be managed by an administrator this way");

        return Result.Success((donation, payment, attempt));
    }
}
