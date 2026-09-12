using Ardalis.Result;
using Hub.Application.Abstractions;
using Hub.Application.Features.Payments.Contracts;
using Hub.Application.Pipelines;
using Hub.Domain.Payments;

namespace Hub.Application.Features.Payments.Commands.ConfirmCashDonation;

sealed class ConfirmCashDonationCommandHandler(
    IPaymentsDbContext paymentsDbContext
) : IRequestHandler<ConfirmCashDonationCommand, DonationResponse>
{
    public async Task<Result<DonationResponse>> Handle(
        ConfirmCashDonationCommand command,
        CancellationToken cancellationToken)
    {
        var loaded = await CashDonationAccess.LoadAsync(
            paymentsDbContext,
            command.DonationId,
            cancellationToken);
        if (!loaded.IsSuccess)
            return loaded.Map();

        var (donation, payment, attempt) = loaded.Value;

        if (donation.Status is DonationStatus.Completed && payment.Status is PaymentStatus.Succeeded)
            return Result.Success(DonationResponse.From(donation, payment));

        if (donation.Status is DonationStatus.Cancelled || payment.Status is PaymentStatus.Cancelled)
            return Result.Error("Cancelled donation cannot be confirmed");

        if (attempt.ProviderPaymentId is null)
            return Result.Error("Cash payment has not been started");

        var applied = DonationCheckout.ApplyProviderResult(
            payment,
            donation,
            attempt.Id,
            attempt.ProviderPaymentId,
            PaymentAttemptStatus.Succeeded);

        if (!applied.IsSuccess)
        {
            await paymentsDbContext.SaveChangesAsync(cancellationToken);
            return applied.Map();
        }

        await paymentsDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(DonationResponse.From(donation, payment));
    }
}
