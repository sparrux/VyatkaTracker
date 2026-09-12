using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ardalis.Result;
using Hub.Application.Abstractions.Payments;
using Hub.Domain.Payments;
using Hub.Domain.Payments.ValueObjects;
using Hub.Infrastructure.Payments.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hub.Infrastructure.Payments.Gateways.PayPal;

sealed class PayPalGateway(
    IHttpClientFactory httpClientFactory,
    PayPalAccessTokenSource accessTokenSource,
    IOptions<PayPalOptions> options,
    ILogger<PayPalGateway> logger
) : IPaymentGateway
{
    public const string HttpClientName = "PayPal";

    public string Name => PaymentGatewayNames.PayPal;

    public async Task<Result<CreateGatewayPaymentResult>> CreatePaymentAsync(
        CreateGatewayPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var configured = options.Value.EnsureConfigured();
        if (!configured.IsSuccess)
            return configured.Map();

        var returnUrl = request.ReturnUrl ?? options.Value.ReturnUrl;
        var cancelUrl = request.CancelUrl ?? options.Value.CancelUrl;
        if (returnUrl is null || cancelUrl is null)
            return Result.Invalid(new ValidationError("PayPal return and cancel URLs must be configured"));

        var body = new PayPalOrderRequest
        {
            Intent = "CAPTURE",
            PurchaseUnits =
            [
                new PayPalPurchaseUnit
                {
                    ReferenceId = request.ReferenceId,
                    CustomId = request.ReferenceId,
                    Description = request.Description,
                    Amount = ToPayPalAmount(request.Amount)
                }
            ],
            PaymentSource = new PayPalPaymentSource
            {
                Paypal = new PayPalWallet
                {
                    ExperienceContext = new PayPalExperienceContext
                    {
                        BrandName = options.Value.BrandName,
                        ShippingPreference = "NO_SHIPPING",
                        UserAction = "PAY_NOW",
                        ReturnUrl = returnUrl.ToString(),
                        CancelUrl = cancelUrl.ToString()
                    }
                }
            }
        };

        var response = await SendAsync<PayPalOrderResponse>(
            HttpMethod.Post,
            "v2/checkout/orders",
            body,
            request.IdempotencyKey,
            cancellationToken);

        if (!response.IsSuccess)
            return response.Map();

        var order = response.Value;
        if (string.IsNullOrWhiteSpace(order.Id))
            return Result.Error("PayPal did not return an order id");

        var approvalUrl = order.Links?
            .FirstOrDefault(link => string.Equals(link.Rel, "payer-action", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(link.Rel, "approve", StringComparison.OrdinalIgnoreCase))
            ?.Href;

        return Result.Success(new CreateGatewayPaymentResult(
            order.Id,
            MapOrderStatus(order.Status, hasApprovalUrl: approvalUrl is not null),
            approvalUrl is null ? null : new Uri(approvalUrl)));
    }

    public async Task<Result<GatewayPaymentResult>> GetPaymentAsync(
        string providerPaymentId,
        CancellationToken cancellationToken)
    {
        var configured = options.Value.EnsureConfigured();
        if (!configured.IsSuccess)
            return configured.Map();

        if (string.IsNullOrWhiteSpace(providerPaymentId))
            return Result.Invalid(new ValidationError("Provider payment id cannot be null or whitespace"));

        var response = await SendAsync<PayPalOrderResponse>(
            HttpMethod.Get,
            $"v2/checkout/orders/{providerPaymentId}",
            body: null,
            idempotencyKey: null,
            cancellationToken);

        if (!response.IsSuccess)
            return response.Map();

        return Result.Success(ToPaymentResult(response.Value, providerPaymentId));
    }

    public async Task<Result<GatewayPaymentResult>> CapturePaymentAsync(
        string providerPaymentId,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var configured = options.Value.EnsureConfigured();
        if (!configured.IsSuccess)
            return configured.Map();

        if (string.IsNullOrWhiteSpace(providerPaymentId))
            return Result.Invalid(new ValidationError("Provider payment id cannot be null or whitespace"));

        var response = await SendAsync<PayPalOrderResponse>(
            HttpMethod.Post,
            $"v2/checkout/orders/{providerPaymentId}/capture",
            body: new { },
            idempotencyKey,
            cancellationToken);

        if (!response.IsSuccess)
            return response.Map();

        return Result.Success(ToPaymentResult(response.Value, providerPaymentId));
    }

    public async Task<Result<RefundGatewayPaymentResult>> RefundAsync(
        RefundGatewayPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var configured = options.Value.EnsureConfigured();
        if (!configured.IsSuccess)
            return configured.Map();

        var captureId = request.ProviderCaptureId;
        if (string.IsNullOrWhiteSpace(captureId))
        {
            var payment = await GetPaymentAsync(request.ProviderPaymentId, cancellationToken);
            if (!payment.IsSuccess)
                return payment.Map();

            captureId = payment.Value.ProviderCaptureId;
        }

        if (string.IsNullOrWhiteSpace(captureId))
            return Result.Error("PayPal capture id is required to refund");

        var response = await SendAsync<PayPalRefundResponse>(
            HttpMethod.Post,
            $"v2/payments/captures/{captureId}/refund",
            new PayPalRefundRequest { Amount = ToPayPalAmount(request.Amount) },
            request.IdempotencyKey,
            cancellationToken);

        if (!response.IsSuccess)
            return response.Map();

        if (string.IsNullOrWhiteSpace(response.Value.Id))
            return Result.Error("PayPal did not return a refund id");

        return Result.Success(new RefundGatewayPaymentResult(
            response.Value.Id,
            MapRefundStatus(response.Value.Status)));
    }

    async Task<Result<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string path,
        object? body,
        string? idempotencyKey,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        var token = await accessTokenSource.GetAccessTokenAsync(cancellationToken);
        if (!token.IsSuccess)
            return token.Map();

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var message = new HttpRequestMessage(method, path);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Headers.TryAddWithoutValidation("Prefer", "return=representation");

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            message.Headers.TryAddWithoutValidation("PayPal-Request-Id", idempotencyKey);

        if (body is not null)
            message.Content = JsonContent.Create(body, options: PayPalJson.Options);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PayPal request {Method} {Path} failed", method, path);
            return Result.Error("PayPal is unavailable");
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "PayPal request {Method} {Path} failed with {StatusCode}",
                method,
                path,
                (int)response.StatusCode);

            return Result.Error(ParseError(payload) ?? $"PayPal request failed with {(int)response.StatusCode}");
        }

        if (string.IsNullOrWhiteSpace(payload))
            return Result.Error("PayPal returned an empty response");

        var parsed = JsonSerializer.Deserialize<TResponse>(payload, PayPalJson.Options);
        if (parsed is null)
            return Result.Error("PayPal returned an unreadable response");

        return Result.Success(parsed);
    }

    static GatewayPaymentResult ToPaymentResult(PayPalOrderResponse order, string fallbackId)
    {
        var capture = order.PurchaseUnits?
            .SelectMany(unit => unit.Payments?.Captures ?? [])
            .FirstOrDefault();

        var status = MapOrderStatus(order.Status, hasApprovalUrl: false);
        if (string.Equals(capture?.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            status = PaymentAttemptStatus.Succeeded;

        return new GatewayPaymentResult(
            order.Id ?? fallbackId,
            status,
            capture?.Id,
            order.Name,
            FirstError(order.Details) ?? order.Message);
    }

    static PaymentAttemptStatus MapOrderStatus(string? status, bool hasApprovalUrl) =>
        status?.ToUpperInvariant() switch
        {
            "COMPLETED" => PaymentAttemptStatus.Succeeded,
            "APPROVED" => PaymentAttemptStatus.Processing,
            "PAYER_ACTION_REQUIRED" => PaymentAttemptStatus.RequiresAction,
            "VOIDED" or "EXPIRED" => PaymentAttemptStatus.Cancelled,
            "CREATED" when hasApprovalUrl => PaymentAttemptStatus.RequiresAction,
            "CREATED" or "SAVED" => PaymentAttemptStatus.Pending,
            _ => hasApprovalUrl ? PaymentAttemptStatus.RequiresAction : PaymentAttemptStatus.Pending
        };

    static RefundStatus MapRefundStatus(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "COMPLETED" => RefundStatus.Succeeded,
            "PENDING" => RefundStatus.Pending,
            "FAILED" or "CANCELLED" => RefundStatus.Failed,
            _ => RefundStatus.Processing
        };

    static PayPalAmount ToPayPalAmount(Money money) =>
        new()
        {
            CurrencyCode = money.Currency.Code,
            Value = FormatAmount(money)
        };

    static string FormatAmount(Money money)
    {
        var decimals = money.Currency.Code is "JPY" or "HUF" or "TWD" ? 0 : 2;
        return money.Amount.ToString($"F{decimals}", CultureInfo.InvariantCulture);
    }

    static string? ParseError(string payload)
    {
        try
        {
            var error = JsonSerializer.Deserialize<PayPalOrderResponse>(payload, PayPalJson.Options);
            return FirstError(error?.Details) ?? error?.Message ?? error?.Name;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    static string? FirstError(IReadOnlyList<PayPalErrorDetail>? details) =>
        details?
            .Select(detail => detail.Description ?? detail.Issue)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
}
