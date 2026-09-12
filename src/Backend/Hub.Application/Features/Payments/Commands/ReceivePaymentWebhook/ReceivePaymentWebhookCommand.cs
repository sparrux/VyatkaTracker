namespace Hub.Application.Features.Payments.Commands.ReceivePaymentWebhook;

public sealed record ReceivePaymentWebhookCommand(
    string Provider,
    string Payload,
    IReadOnlyDictionary<string, string> Headers);
