using Ardalis.Result;
using Hub.Application.Abstractions;
using Hub.Application.Abstractions.Payments;
using Hub.Application.Features.Common.Contracts;
using Hub.Application.Features.Payments;
using Hub.Application.Pipelines;
using Hub.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace Hub.Application.Features.Payments.Commands.ProcessPaymentWebhook;

sealed class ProcessPaymentWebhookCommandHandler(
    IPaymentsDbContext paymentsDbContext,
    IPaymentGatewayResolver gatewayResolver
) : IRequestHandler<ProcessPaymentWebhookCommand, IdResponse>
{
    public async Task<Result<IdResponse>> Handle(
        ProcessPaymentWebhookCommand command,
        CancellationToken cancellationToken)
    {
        var webhook = await paymentsDbContext.WebhookEvents
            .FirstOrDefaultAsync(x => x.Id == command.WebhookEventId, cancellationToken);

        if (webhook is null)
            return Result.NotFound("Payment webhook event not found");

        if (webhook.Status == WebhookProcessingStatus.Processed)
            return Result.Success(new IdResponse(webhook.Id));

        if (string.IsNullOrWhiteSpace(webhook.ProviderPaymentId))
            return await CompleteWebhook(
                webhook,
                webhook.MarkAsFailed("Webhook does not reference a provider payment"),
                cancellationToken);

        var payment = await paymentsDbContext.Payments
            .Include(x => x.Attempts)
            .Include(x => x.Refunds)
            .FirstOrDefaultAsync(
                x => x.Attempts.Any(attempt => attempt.ProviderPaymentId == webhook.ProviderPaymentId),
                cancellationToken);

        if (payment is null)
            return Result.Error("Payment not found for provider payment id");

        var attempt = payment.Attempts
            .Where(x => x.ProviderPaymentId == webhook.ProviderPaymentId)
            .OrderByDescending(x => x.AttemptNumber)
            .FirstOrDefault();

        if (attempt is null)
            return Result.Error("Payment attempt not found for provider payment id");

        var gateway = gatewayResolver.Resolve(attempt.Provider.Value);
        if (!gateway.IsSuccess)
            return gateway.Map();

        if (!gateway.Value.SupportsRemoteCapture)
            return await CompleteWebhook(
                webhook,
                webhook.MarkAsFailed("Webhook processing is not supported for this payment provider"),
                cancellationToken);

        var current = await gateway.Value.GetPaymentAsync(
            webhook.ProviderPaymentId,
            cancellationToken);
        if (!current.IsSuccess)
            return current.Map();

        Donation? donation = null;
        if (payment.Purpose == PaymentPurpose.Donation)
        {
            donation = await paymentsDbContext.Donations
                .FirstOrDefaultAsync(x => x.Id == payment.ReferenceId, cancellationToken);
            if (donation is null)
                return Result.Error("Donation not found for payment");
        }

        var applied = DonationCheckout.ApplyProviderResult(
            payment,
            donation,
            attempt.Id,
            current.Value.ProviderPaymentId,
            current.Value.Status,
            current.Value.FailureCode,
            current.Value.FailureMessage);
        if (!applied.IsSuccess)
            return await CompleteWebhook(webhook, webhook.MarkAsFailed(applied.Errors.FirstOrDefault()), cancellationToken);

        return await CompleteWebhook(webhook, webhook.MarkAsProcessed(), cancellationToken);
    }

    async Task<Result<IdResponse>> CompleteWebhook(
        PaymentWebhookEvent webhook,
        Result marked,
        CancellationToken cancellationToken)
    {
        if (!marked.IsSuccess)
            return marked.Map();

        await paymentsDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(new IdResponse(webhook.Id));
    }
}
