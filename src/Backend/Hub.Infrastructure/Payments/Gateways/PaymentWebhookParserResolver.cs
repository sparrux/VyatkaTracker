using Ardalis.Result;
using Hub.Application.Abstractions.Payments;

namespace Hub.Infrastructure.Payments.Gateways;

sealed class PaymentWebhookParserResolver(
    IEnumerable<IPaymentWebhookParser> parsers
) : IPaymentWebhookParserResolver
{
    readonly IReadOnlyDictionary<string, IPaymentWebhookParser> _parsers = parsers.ToDictionary(
        parser => parser.Provider,
        parser => parser,
        StringComparer.OrdinalIgnoreCase);

    public Result<IPaymentWebhookParser> Resolve(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return Result.Invalid(new ValidationError("Payment webhook provider is required"));

        if (_parsers.TryGetValue(provider.Trim(), out var parser))
            return Result.Success(parser);

        var registered = string.Join(", ", _parsers.Keys);
        return Result.NotFound(
            $"Payment webhook parser '{provider}' is not registered. Available: {registered}");
    }
}
