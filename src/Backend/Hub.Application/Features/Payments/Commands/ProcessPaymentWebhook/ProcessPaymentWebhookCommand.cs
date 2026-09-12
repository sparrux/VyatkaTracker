namespace Hub.Application.Features.Payments.Commands.ProcessPaymentWebhook;

public sealed record ProcessPaymentWebhookCommand(Guid WebhookEventId);
