using Ardalis.Result;
using Hub.Application.Abstractions;
using Hub.Application.Abstractions.Messaging;
using Hub.Application.Abstractions.Payments;
using Hub.Application.Features.Common.Contracts;
using Hub.Application.Pipelines;
using Hub.Domain.Payments;
using Hub.Domain.Payments.ValueObjects;
using Microsoft.EntityFrameworkCore;
using ProcessPaymentWebhookMessage = Hub.Application.Features.Payments.Messages.ProcessPaymentWebhook;

namespace Hub.Application.Features.Payments.Commands.ReceivePaymentWebhook;

sealed class ReceivePaymentWebhookCommandHandler(
    IPaymentsDbContext paymentsDbContext,
    IPaymentWebhookParserResolver parserResolver,
    IIntegrationEventPublisher publisher
) : IRequestHandler<ReceivePaymentWebhookCommand, IdResponse>
{
    public async Task<Result<IdResponse>> Handle(
        ReceivePaymentWebhookCommand command,
        CancellationToken cancellationToken)
    {
        var parser = parserResolver.Resolve(command.Provider);
        if (!parser.IsSuccess)
            return parser.Map();

        var parsed = await parser.Value.ParseAsync(
            command.Payload,
            command.Headers,
            cancellationToken);
        if (!parsed.IsSuccess)
            return parsed.Map();

        var provider = ProviderName.Create(parser.Value.Provider);
        if (!provider.IsSuccess)
            return provider.Map();

        var existing = await paymentsDbContext.WebhookEvents
            .FirstOrDefaultAsync(
                webhook => webhook.ProviderEventId == parsed.Value.ProviderEventId
                           && EF.Property<string>(webhook, "Provider") == provider.Value.Value,
                cancellationToken);

        if (existing is not null)
            return await EnqueueIfNeeded(existing, cancellationToken);

        var received = PaymentWebhookEvent.Receive(
            provider.Value,
            parsed.Value.ProviderEventId,
            parsed.Value.EventType,
            command.Payload,
            parsed.Value.ProviderPaymentId);
        if (!received.IsSuccess)
            return received.Map();

        await paymentsDbContext.WebhookEvents.AddAsync(received.Value, cancellationToken);
        await publisher.Publish(ToMessage(received.Value), cancellationToken);

        try
        {
            await paymentsDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            paymentsDbContext.RejectChanges();

            var duplicate = await paymentsDbContext.WebhookEvents
                .FirstOrDefaultAsync(
                    webhook => webhook.ProviderEventId == parsed.Value.ProviderEventId
                               && EF.Property<string>(webhook, "Provider") == provider.Value.Value,
                    cancellationToken);

            if (duplicate is null)
                throw;

            return await EnqueueIfNeeded(duplicate, cancellationToken);
        }

        return Result.Success(new IdResponse(received.Value.Id));
    }

    async Task<Result<IdResponse>> EnqueueIfNeeded(
        PaymentWebhookEvent webhook,
        CancellationToken cancellationToken)
    {
        if (webhook.Status == WebhookProcessingStatus.Processed)
            return Result.Success(new IdResponse(webhook.Id));

        if (webhook.Status == WebhookProcessingStatus.Failed)
        {
            var retried = webhook.Retry();
            if (!retried.IsSuccess)
                return retried.Map();
        }

        await publisher.Publish(ToMessage(webhook), cancellationToken);
        await paymentsDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(new IdResponse(webhook.Id));
    }

    static ProcessPaymentWebhookMessage ToMessage(PaymentWebhookEvent webhook) =>
        new(
            webhook.Id,
            webhook.Provider.Value,
            webhook.ProviderEventId,
            webhook.EventType,
            webhook.ProviderPaymentId);
}
