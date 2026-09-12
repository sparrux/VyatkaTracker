using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ardalis.Result;
using Hub.Application.Abstractions.Payments;
using Hub.Infrastructure.Payments.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hub.Infrastructure.Payments.Gateways.PayPal;

sealed class PayPalWebhookParser(
    IHttpClientFactory httpClientFactory,
    PayPalAccessTokenSource accessTokenSource,
    IOptions<PayPalOptions> options,
    IHostEnvironment environment,
    ILogger<PayPalWebhookParser> logger
) : IPaymentWebhookParser
{
    public string Provider => PaymentGatewayNames.PayPal;

    public async Task<Result<ParsedPaymentWebhook>> ParseAsync(
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        var configured = options.Value.EnsureConfigured();
        if (!configured.IsSuccess)
            return configured.Map();

        if (!string.IsNullOrWhiteSpace(options.Value.WebhookId))
        {
            var verified = await VerifyAsync(payload, headers, cancellationToken);
            if (!verified.IsSuccess)
                return verified.Map();
        }
        else if (!environment.IsDevelopment())
        {
            return Result.Error("PayPal webhook verification is not configured");
        }
        else
        {
            logger.LogWarning("PayPal webhook signature verification is skipped in Development");
        }

        return ParsePayload(payload);
    }

    async Task<Result> VerifyAsync(
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        var token = await accessTokenSource.GetAccessTokenAsync(cancellationToken);
        if (!token.IsSuccess)
            return token.Map();

        if (!TryGetHeader(headers, "PAYPAL-AUTH-ALGO", out var authAlgo)
            || !TryGetHeader(headers, "PAYPAL-CERT-URL", out var certUrl)
            || !TryGetHeader(headers, "PAYPAL-TRANSMISSION-ID", out var transmissionId)
            || !TryGetHeader(headers, "PAYPAL-TRANSMISSION-SIG", out var transmissionSig)
            || !TryGetHeader(headers, "PAYPAL-TRANSMISSION-TIME", out var transmissionTime))
        {
            return Result.Error("PayPal webhook signature headers are missing");
        }

        JsonElement webhookEvent;
        try
        {
            webhookEvent = JsonSerializer.Deserialize<JsonElement>(payload, PayPalJson.Options);
        }
        catch (JsonException)
        {
            return Result.Error("PayPal webhook payload is not valid JSON");
        }

        var body = new PayPalWebhookVerifyRequest
        {
            AuthAlgo = authAlgo,
            CertUrl = certUrl,
            TransmissionId = transmissionId,
            TransmissionSig = transmissionSig,
            TransmissionTime = transmissionTime,
            WebhookId = options.Value.WebhookId,
            WebhookEvent = webhookEvent
        };

        var client = httpClientFactory.CreateClient(PayPalGateway.HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/notifications/verify-webhook-signature");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        request.Content = JsonContent.Create(body, options: PayPalJson.Options);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PayPal webhook verification request failed");
            return Result.Error("PayPal is unavailable");
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "PayPal webhook verification failed with {StatusCode}",
                (int)response.StatusCode);
            return Result.Error("PayPal webhook signature is invalid");
        }

        var verification = await response.Content.ReadFromJsonAsync<PayPalWebhookVerifyResponse>(
            PayPalJson.Options,
            cancellationToken);

        if (!string.Equals(verification?.VerificationStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            return Result.Error("PayPal webhook signature is invalid");

        return Result.Success();
    }

    static Result<ParsedPaymentWebhook> ParsePayload(string payload)
    {
        PayPalWebhookNotification? notification;
        try
        {
            notification = JsonSerializer.Deserialize<PayPalWebhookNotification>(payload, PayPalJson.Options);
        }
        catch (JsonException)
        {
            return Result.Error("PayPal webhook payload is not valid JSON");
        }

        if (notification is null || string.IsNullOrWhiteSpace(notification.Id))
            return Result.Invalid(new ValidationError("PayPal webhook event id is missing"));

        if (string.IsNullOrWhiteSpace(notification.EventType))
            return Result.Invalid(new ValidationError("PayPal webhook event type is missing"));

        return Result.Success(new ParsedPaymentWebhook(
            notification.Id,
            notification.EventType,
            ExtractProviderPaymentId(notification)));
    }

    static string? ExtractProviderPaymentId(PayPalWebhookNotification notification)
    {
        if (string.Equals(notification.ResourceType, "checkout-order", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(notification.Resource?.Id))
        {
            return notification.Resource.Id;
        }

        var orderId = notification.Resource?.SupplementaryData?.RelatedIds?.OrderId;
        if (!string.IsNullOrWhiteSpace(orderId))
            return orderId;

        return string.IsNullOrWhiteSpace(notification.Resource?.Id) ? null : notification.Resource.Id;
    }

    static bool TryGetHeader(
        IReadOnlyDictionary<string, string> headers,
        string name,
        out string value)
    {
        foreach (var header in headers)
        {
            if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(header.Value))
            {
                value = header.Value;
                return true;
            }
        }

        value = "";
        return false;
    }
}
