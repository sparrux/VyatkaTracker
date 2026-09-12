using Hub.Application.Features.Payments.Messages;
using Hub.Domain.Common.DomainEvents;
using Hub.Domain.Payments.Events;

namespace Hub.Application.Features.Payments.Mapping;

public static class PaymentIntegrationEvents
{
    public static IReadOnlyList<object> From(IEnumerable<IDomainEvent> domainEvents)
    {
        var messages = new List<object>();

        foreach (var domainEvent in domainEvents)
        {
            switch (domainEvent)
            {
                case PaymentSucceededEvent succeeded:
                    messages.Add(new PaymentSucceeded(
                        succeeded.EventId,
                        succeeded.PaymentId,
                        succeeded.CustomerId,
                        succeeded.Purpose.ToString(),
                        succeeded.ReferenceId,
                        succeeded.Amount.Amount,
                        succeeded.Amount.Currency.Code,
                        succeeded.OccurredOn));
                    break;

                case PaymentFailedEvent failed:
                    messages.Add(new PaymentFailed(
                        failed.EventId,
                        failed.PaymentId,
                        failed.Purpose.ToString(),
                        failed.ReferenceId,
                        failed.Reason,
                        failed.OccurredOn));
                    break;

                case RefundSucceededEvent refunded:
                    messages.Add(new RefundSucceeded(
                        refunded.EventId,
                        refunded.PaymentId,
                        refunded.RefundId,
                        refunded.Amount.Amount,
                        refunded.Amount.Currency.Code,
                        refunded.OccurredOn));
                    break;
            }
        }

        return messages;
    }
}
