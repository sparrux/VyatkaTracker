using System.Text.Json;

namespace Hub.Infrastructure.Payments.Gateways.PayPal;

sealed record PayPalWebhookNotification
{
    public string? Id { get; init; }
    public string? EventType { get; init; }
    public string? ResourceType { get; init; }
    public PayPalWebhookResource? Resource { get; init; }
}

sealed record PayPalWebhookResource
{
    public string? Id { get; init; }
    public PayPalWebhookSupplementaryData? SupplementaryData { get; init; }
}

sealed record PayPalWebhookSupplementaryData
{
    public PayPalWebhookRelatedIds? RelatedIds { get; init; }
}

sealed record PayPalWebhookRelatedIds
{
    public string? OrderId { get; init; }
}

sealed record PayPalWebhookVerifyRequest
{
    public required string AuthAlgo { get; init; }
    public required string CertUrl { get; init; }
    public required string TransmissionId { get; init; }
    public required string TransmissionSig { get; init; }
    public required string TransmissionTime { get; init; }
    public string? WebhookId { get; init; }
    public JsonElement WebhookEvent { get; init; }
}

sealed record PayPalWebhookVerifyResponse
{
    public string? VerificationStatus { get; init; }
}
