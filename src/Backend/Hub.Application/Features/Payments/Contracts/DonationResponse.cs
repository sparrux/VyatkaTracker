using Hub.Domain.Payments;
using Hub.Domain.Payments.ValueObjects;

namespace Hub.Application.Features.Payments.Contracts;

public sealed record DonationResponse(
    Guid Id,
    DonationStatus Status,
    MoneyResponse Amount,
    bool IsAnonymous,
    Guid? EventId,
    Guid? PaymentId,
    PaymentStatus? PaymentStatus,
    string? Provider,
    Uri? ApprovalUrl,
    string? FailureReason
)
{
    public static DonationResponse From(
        Donation donation,
        Payment? payment,
        Uri? approvalUrl = null)
    {
        var attempt = payment?.Attempts
            .OrderByDescending(x => x.AttemptNumber)
            .FirstOrDefault();

        return new DonationResponse(
            donation.Id,
            donation.Status,
            new MoneyResponse(donation.Amount.Amount, donation.Amount.Currency.Code),
            donation.IsAnonymous,
            donation.Reference?.Type == BusinessReferenceTypes.EventContribution
                ? donation.Reference.Id
                : null,
            donation.PaymentId,
            payment?.Status,
            attempt?.Provider.Value,
            approvalUrl,
            payment?.FailureReason);
    }
}
