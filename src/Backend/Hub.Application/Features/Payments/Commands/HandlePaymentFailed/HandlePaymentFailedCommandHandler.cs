using Ardalis.Result;
using Hub.Application.Abstractions;
using Hub.Application.Features.Common.Contracts;
using Hub.Application.Pipelines;
using Hub.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hub.Application.Features.Payments.Commands.HandlePaymentFailed;

sealed class HandlePaymentFailedCommandHandler(
    IPaymentsDbContext paymentsDbContext,
    ILogger<HandlePaymentFailedCommandHandler> logger
) : IRequestHandler<HandlePaymentFailedCommand, IdResponse>
{
    public async Task<Result<IdResponse>> Handle(
        HandlePaymentFailedCommand command,
        CancellationToken cancellationToken)
    {
        if (!IsDonation(command.Purpose))
            return Result.Success(new IdResponse(command.PaymentId));

        var donation = await paymentsDbContext.Donations
            .FirstOrDefaultAsync(x => x.Id == command.ReferenceId, cancellationToken);

        if (donation is null)
        {
            logger.LogWarning(
                "Donation {DonationId} was not found for failed payment {PaymentId}",
                command.ReferenceId,
                command.PaymentId);
            return Result.Success(new IdResponse(command.PaymentId));
        }

        var failed = donation.Fail();
        if (!failed.IsSuccess)
        {
            logger.LogWarning(
                "Donation {DonationId} was not failed after payment {PaymentId}: {Error}",
                donation.Id,
                command.PaymentId,
                failed.Errors.FirstOrDefault());
            return Result.Success(new IdResponse(command.PaymentId));
        }

        await paymentsDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(new IdResponse(command.PaymentId));
    }

    static bool IsDonation(string purpose) =>
        string.Equals(purpose, nameof(PaymentPurpose.Donation), StringComparison.OrdinalIgnoreCase);
}
