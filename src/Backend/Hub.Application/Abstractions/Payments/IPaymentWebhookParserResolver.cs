using Ardalis.Result;

namespace Hub.Application.Abstractions.Payments;

public interface IPaymentWebhookParserResolver
{
    Result<IPaymentWebhookParser> Resolve(string provider);
}
