using Ardalis.Result;
using Hub.Application.Abstractions.Payments;
using Hub.Domain.Payments;

namespace Hub.Infrastructure.Payments.Gateways.Cash;

sealed class CashGateway : IPaymentGateway
{
    public string Name => PaymentGatewayNames.Cash;

    public bool SupportsRemoteCapture => false;

    public Task<Result<CreateGatewayPaymentResult>> CreatePaymentAsync(
        CreateGatewayPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReferenceId))
            return Task.FromResult<Result<CreateGatewayPaymentResult>>(
                Result.Invalid(new ValidationError("Cash payment reference id is required")));

        var providerPaymentId = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? $"cash_{request.ReferenceId}"
            : $"cash_{request.IdempotencyKey}";

        return Task.FromResult(Result.Success(new CreateGatewayPaymentResult(
            providerPaymentId,
            PaymentAttemptStatus.RequiresAction,
            ApprovalUrl: null)));
    }

    public Task<Result<GatewayPaymentResult>> GetPaymentAsync(
        string providerPaymentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId))
            return Task.FromResult<Result<GatewayPaymentResult>>(
                Result.Invalid(new ValidationError("Provider payment id cannot be null or whitespace")));

        return Task.FromResult(Result.Success(new GatewayPaymentResult(
            providerPaymentId.Trim(),
            PaymentAttemptStatus.RequiresAction,
            ProviderCaptureId: null,
            FailureCode: null,
            FailureMessage: null)));
    }

    public Task<Result<GatewayPaymentResult>> CapturePaymentAsync(
        string providerPaymentId,
        string? idempotencyKey,
        CancellationToken cancellationToken) =>
        Task.FromResult<Result<GatewayPaymentResult>>(
            Result.Error("Cash payments are confirmed by an administrator"));

    public Task<Result<RefundGatewayPaymentResult>> RefundAsync(
        RefundGatewayPaymentRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult<Result<RefundGatewayPaymentResult>>(
            Result.Error("Cash refunds are recorded by an administrator"));
}
