using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Ardalis.Result;
using Hub.Infrastructure.Payments.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hub.Infrastructure.Payments.Gateways.PayPal;

sealed class PayPalAccessTokenSource(
    IHttpClientFactory httpClientFactory,
    IOptions<PayPalOptions> options,
    IMemoryCache cache,
    ILogger<PayPalAccessTokenSource> logger
)
{
    public const string TokenCacheKey = "payments:paypal:access-token";

    public async Task<Result<string>> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(TokenCacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
            return Result.Success(cached);

        var client = httpClientFactory.CreateClient(PayPalGateway.HttpClientName);
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{options.Value.ClientId}:{options.Value.ClientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/oauth2/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        ]);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PayPal token request failed");
            return Result.Error("PayPal is unavailable");
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "PayPal token request failed with {StatusCode}: {Payload}",
                (int)response.StatusCode,
                payload);
            return Result.Error(
                ParseTokenError(payload) ?? $"PayPal authentication failed ({(int)response.StatusCode})");
        }

        var token = JsonSerializer.Deserialize<PayPalTokenResponse>(payload, PayPalJson.Options);
        if (token?.AccessToken is null)
            return Result.Error("PayPal did not return an access token");

        var lifetime = token.ExpiresIn > 60
            ? TimeSpan.FromSeconds(token.ExpiresIn - 60)
            : TimeSpan.FromMinutes(1);

        cache.Set(TokenCacheKey, token.AccessToken, lifetime);
        return Result.Success(token.AccessToken);
    }

    static string? ParseTokenError(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (root.TryGetProperty("error_description", out var description))
                return description.GetString();
            if (root.TryGetProperty("message", out var message))
                return message.GetString();
            if (root.TryGetProperty("error", out var error))
                return error.GetString();
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
