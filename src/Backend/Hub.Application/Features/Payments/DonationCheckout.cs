using Ardalis.Result;
using Hub.Application.Abstractions.Payments;
using Hub.Domain.Payments;

namespace Hub.Application.Features.Payments;

static class DonationCheckout
{
    public static Result ApplyProviderResult(
        Payment payment,
        Donation? donation,
        Guid attemptId,
        string providerPaymentId,
        PaymentAttemptStatus status,
        string? failureCode = null,
        string? failureMessage = null)
    {
        var assigned = payment.AssignAttemptProviderId(attemptId, providerPaymentId);
        if (!assigned.IsSuccess)
            return assigned;

        return ApplyGatewayStatus(
            payment,
            donation,
            attemptId,
            status,
            failureCode,
            failureMessage);
    }

    public static Result ApplyGatewayStatus(
        Payment payment,
        Donation? donation,
        Guid attemptId,
        PaymentAttemptStatus status,
        string? failureCode = null,
        string? failureMessage = null)
    {
        var applied = status switch
        {
            PaymentAttemptStatus.Pending => Result.Success(),
            PaymentAttemptStatus.RequiresAction => payment.MarkAttemptRequiresAction(attemptId),
            PaymentAttemptStatus.Processing => payment.MarkAttemptProcessing(attemptId),
            PaymentAttemptStatus.Succeeded => payment.SucceedAttempt(attemptId),
            PaymentAttemptStatus.Failed => payment.FailAttempt(attemptId, failureCode, failureMessage),
            PaymentAttemptStatus.Cancelled => payment.Cancel(),
            _ => Result.Error($"Unsupported payment attempt status '{status}'")
        };

        if (!applied.IsSuccess)
            return applied;

        if (donation is null)
            return Result.Success();

        return status switch
        {
            PaymentAttemptStatus.Succeeded => donation.Complete(),
            PaymentAttemptStatus.Failed => donation.Fail(),
            PaymentAttemptStatus.Cancelled => donation.Cancel(),
            _ => Result.Success()
        };
    }

    public static string GatewayIdempotencyKey(PaymentAttempt attempt, string operation)
    {
        var suffix = operation switch
        {
            "create" => "cr",
            "capture" => "cp",
            _ => "op"
        };

        return $"{attempt.Id:N}-{suffix}";
    }

    public static bool IsCash(PaymentAttempt attempt) =>
        string.Equals(attempt.Provider.Value, PaymentGatewayNames.Cash, StringComparison.OrdinalIgnoreCase);
}
