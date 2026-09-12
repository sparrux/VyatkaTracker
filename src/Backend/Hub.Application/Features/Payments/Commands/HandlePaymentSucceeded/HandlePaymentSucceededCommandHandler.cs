using Ardalis.Result;
using Hub.Application.Abstractions;
using Hub.Application.Features.Common.Contracts;
using Hub.Application.Pipelines;
using Hub.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hub.Application.Features.Payments.Commands.HandlePaymentSucceeded;

sealed class HandlePaymentSucceededCommandHandler(
    IPaymentsDbContext paymentsDbContext,
    ILogger<HandlePaymentSucceededCommandHandler> logger
) : IRequestHandler<HandlePaymentSucceededCommand, IdResponse>
{
    public async Task<Result<IdResponse>> Handle(
        HandlePaymentSucceededCommand command,
        CancellationToken cancellationToken)
    {
        if (!IsDonation(command.Purpose))
            return Result.Success(new IdResponse(command.PaymentId));

        var donation = await paymentsDbContext.Donations
            .FirstOrDefaultAsync(x => x.Id == command.ReferenceId, cancellationToken);

        if (donation is null)
        {
            logger.LogWarning(
                "Donation {DonationId} was not found for succeeded payment {PaymentId}",
                command.ReferenceId,
                command.PaymentId);
            return Result.Success(new IdResponse(command.PaymentId));
        }

        var completed = donation.Complete();
        if (!completed.IsSuccess)
        {
            logger.LogWarning(
                "Donation {DonationId} was not completed after payment {PaymentId}: {Error}",
                donation.Id,
                command.PaymentId,
                completed.Errors.FirstOrDefault());
            return Result.Success(new IdResponse(command.PaymentId));
        }

        await paymentsDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(new IdResponse(command.PaymentId));
    }

    static bool IsDonation(string purpose) =>
        string.Equals(purpose, nameof(PaymentPurpose.Donation), StringComparison.OrdinalIgnoreCase);
}
