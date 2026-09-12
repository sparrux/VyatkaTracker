using Ardalis.Result;
using Hub.Application.Abstractions;
using Hub.Application.Abstractions.Payments;
using Hub.Application.Features.Payments.Contracts;
using Hub.Application.Pipelines;
using Hub.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace Hub.Application.Features.Payments.Commands.ConfirmDonation;

sealed class ConfirmDonationCommandHandler(
    IPaymentsDbContext paymentsDbContext,
    IPaymentGatewayResolver gatewayResolver
) : IRequestHandler<ConfirmDonationCommand, DonationResponse>
{
    public async Task<Result<DonationResponse>> Handle(
        ConfirmDonationCommand command,
        CancellationToken cancellationToken)
    {
        var donation = await paymentsDbContext.Donations
            .FirstOrDefaultAsync(
                x => x.Id == command.DonationId && x.CustomerId == command.UserId,
                cancellationToken);

        if (donation is null)
            return Result.NotFound("Donation not found");

        if (donation.PaymentId is null)
            return Result.Error("Donation has no payment attached");

        var payment = await paymentsDbContext.Payments
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.Id == donation.PaymentId, cancellationToken);

        if (payment is null)
            return Result.Error("Payment not found for donation");

        if (donation.Status is DonationStatus.Completed && payment.Status is PaymentStatus.Succeeded)
            return Result.Success(DonationResponse.From(donation, payment));

        if (donation.Status is DonationStatus.Cancelled || payment.Status is PaymentStatus.Cancelled)
            return Result.Error("Cancelled donation cannot be confirmed");

        var attempt = payment.Attempts
            .OrderByDescending(x => x.AttemptNumber)
            .FirstOrDefault();

        if (attempt?.ProviderPaymentId is null)
            return Result.Error("Donation payment has not been started with a provider");

        var gateway = gatewayResolver.Resolve(attempt.Provider.Value);
        if (!gateway.IsSuccess)
            return gateway.Map();

        var captured = await gateway.Value.CapturePaymentAsync(
            attempt.ProviderPaymentId,
            DonationCheckout.GatewayIdempotencyKey(attempt, "capture"),
            cancellationToken);

        var result = captured;
        if (!result.IsSuccess)
        {
            var current = await gateway.Value.GetPaymentAsync(attempt.ProviderPaymentId, cancellationToken);
            if (!current.IsSuccess)
                return captured.Map();

            result = current;
        }

        var applied = DonationCheckout.ApplyProviderResult(
            payment,
            donation,
            attempt.Id,
            result.Value.ProviderPaymentId,
            result.Value.Status,
            result.Value.FailureCode,
            result.Value.FailureMessage);

        if (!applied.IsSuccess)
        {
            await paymentsDbContext.SaveChangesAsync(cancellationToken);
            return applied.Map();
        }

        await paymentsDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(DonationResponse.From(donation, payment));
    }
}
