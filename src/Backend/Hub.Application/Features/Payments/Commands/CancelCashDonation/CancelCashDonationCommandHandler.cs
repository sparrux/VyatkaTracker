using Ardalis.Result;
using Hub.Application.Abstractions;
using Hub.Application.Features.Payments.Contracts;
using Hub.Application.Pipelines;
using Hub.Domain.Payments;

namespace Hub.Application.Features.Payments.Commands.CancelCashDonation;

sealed class CancelCashDonationCommandHandler(
    IPaymentsDbContext paymentsDbContext
) : IRequestHandler<CancelCashDonationCommand, DonationResponse>
{
    public async Task<Result<DonationResponse>> Handle(
        CancelCashDonationCommand command,
        CancellationToken cancellationToken)
    {
        var loaded = await CashDonationAccess.LoadAsync(
            paymentsDbContext,
            command.DonationId,
            cancellationToken);
        if (!loaded.IsSuccess)
            return loaded.Map();

        var (donation, payment, attempt) = loaded.Value;

        if (donation.Status is DonationStatus.Cancelled || payment.Status is PaymentStatus.Cancelled)
            return Result.Success(DonationResponse.From(donation, payment));

        if (donation.Status is DonationStatus.Completed || payment.Status is PaymentStatus.Succeeded)
            return Result.Error("Succeeded cash donation cannot be cancelled");

        var applied = DonationCheckout.ApplyGatewayStatus(
            payment,
            donation,
            attempt.Id,
            PaymentAttemptStatus.Cancelled);

        if (!applied.IsSuccess)
        {
            await paymentsDbContext.SaveChangesAsync(cancellationToken);
            return applied.Map();
        }

        await paymentsDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(DonationResponse.From(donation, payment));
    }
}
