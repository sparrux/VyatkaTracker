using Ardalis.Result;
using Hub.Domain.Payments;
using Hub.Domain.Payments.ValueObjects;

namespace Hub.Application.Abstractions.Payments;

public interface IPaymentGateway
{
    string Name { get; }

    bool SupportsRemoteCapture { get; }

    Task<Result<CreateGatewayPaymentResult>> CreatePaymentAsync(
        CreateGatewayPaymentRequest request,
        CancellationToken cancellationToken);

    Task<Result<GatewayPaymentResult>> GetPaymentAsync(
        string providerPaymentId,
        CancellationToken cancellationToken);

    Task<Result<GatewayPaymentResult>> CapturePaymentAsync(
        string providerPaymentId,
        string? idempotencyKey,
        CancellationToken cancellationToken);

    Task<Result<RefundGatewayPaymentResult>> RefundAsync(
        RefundGatewayPaymentRequest request,
        CancellationToken cancellationToken);
}

public sealed record CreateGatewayPaymentRequest(
    Money Amount,
    string ReferenceId,
    string? Description,
    string? IdempotencyKey,
    Uri? ReturnUrl,
    Uri? CancelUrl);

public sealed record CreateGatewayPaymentResult(
    string ProviderPaymentId,
    PaymentAttemptStatus Status,
    Uri? ApprovalUrl);

public sealed record GatewayPaymentResult(
    string ProviderPaymentId,
    PaymentAttemptStatus Status,
    string? ProviderCaptureId,
    string? FailureCode,
    string? FailureMessage);

public sealed record RefundGatewayPaymentRequest(
    string ProviderPaymentId,
    Money Amount,
    string? ProviderCaptureId,
    string? IdempotencyKey);

public sealed record RefundGatewayPaymentResult(
    string ProviderRefundId,
    RefundStatus Status);
