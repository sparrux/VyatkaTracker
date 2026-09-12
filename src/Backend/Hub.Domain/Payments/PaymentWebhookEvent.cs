using System.Diagnostics.CodeAnalysis;
using Ardalis.Result;
using Hub.Domain.Common;
using Hub.Domain.Payments.ValueObjects;

#pragma warning disable CS8618

namespace Hub.Domain.Payments;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed class PaymentWebhookEvent : AggregateRoot
{
    PaymentWebhookEvent() { }

    PaymentWebhookEvent(
        ProviderName provider,
        string providerEventId,
        string eventType,
        string payload,
        string? providerPaymentId)
    {
        Id = Guid.NewGuid();
        Provider = provider;
        ProviderEventId = providerEventId;
        EventType = eventType;
        Payload = payload;
        ProviderPaymentId = providerPaymentId;
        Status = WebhookProcessingStatus.Received;
    }

    public ProviderName Provider { get; private set; }
    public string ProviderEventId { get; private set; }
    public string EventType { get; private set; }
    public string Payload { get; private set; }
    public string? ProviderPaymentId { get; private set; }
    public WebhookProcessingStatus Status { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public static Result<PaymentWebhookEvent> Receive(
        ProviderName provider,
        string providerEventId,
        string eventType,
        string payload,
        string? providerPaymentId)
    {
        if (string.IsNullOrWhiteSpace(providerEventId))
            return Result.Invalid(new ValidationError("Provider event id cannot be null or whitespace"));

        if (string.IsNullOrWhiteSpace(eventType))
            return Result.Invalid(new ValidationError("Webhook event type cannot be null or whitespace"));

        if (string.IsNullOrWhiteSpace(payload))
            return Result.Invalid(new ValidationError("Webhook payload cannot be null or whitespace"));

        var normalizedProviderPaymentId = string.IsNullOrWhiteSpace(providerPaymentId)
            ? null
            : providerPaymentId.Trim();

        return Result.Success(new PaymentWebhookEvent(
            provider,
            providerEventId.Trim(),
            eventType.Trim(),
            payload,
            normalizedProviderPaymentId));
    }

    public Result MarkAsProcessed()
    {
        if (Status == WebhookProcessingStatus.Processed)
            return Result.Success();

        Status = WebhookProcessingStatus.Processed;
        ProcessedAt = DateTimeOffset.UtcNow;
        FailureReason = null;
        return Result.Success();
    }

    public Result MarkAsFailed(string? reason)
    {
        if (Status == WebhookProcessingStatus.Processed)
            return Result.Error("Processed webhook cannot be marked as failed");

        Status = WebhookProcessingStatus.Failed;
        FailureReason = reason;
        return Result.Success();
    }

    public Result Retry()
    {
        if (Status == WebhookProcessingStatus.Processed)
            return Result.Error("Processed webhook does not need a retry");

        Status = WebhookProcessingStatus.Received;
        FailureReason = null;
        return Result.Success();
    }
}
