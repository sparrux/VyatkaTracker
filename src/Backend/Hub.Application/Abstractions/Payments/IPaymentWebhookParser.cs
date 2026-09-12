using Ardalis.Result;

namespace Hub.Application.Abstractions.Payments;

public interface IPaymentWebhookParser
{
    string Provider { get; }

    Task<Result<ParsedPaymentWebhook>> ParseAsync(
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken);
}

public sealed record ParsedPaymentWebhook(
    string ProviderEventId,
    string EventType,
    string? ProviderPaymentId);
