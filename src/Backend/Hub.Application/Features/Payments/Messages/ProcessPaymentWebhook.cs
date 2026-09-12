namespace Hub.Application.Features.Payments.Messages;

public sealed record ProcessPaymentWebhook(
    Guid WebhookEventId,
    string Provider,
    string ProviderEventId,
    string EventType,
    string? ProviderPaymentId);
