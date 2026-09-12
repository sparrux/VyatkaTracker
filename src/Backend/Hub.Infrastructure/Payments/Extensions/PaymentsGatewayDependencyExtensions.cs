using System.Security.Authentication;
using Hub.Application.Abstractions.Payments;
using Hub.Infrastructure.Payments.Gateways;
using Hub.Infrastructure.Payments.Gateways.PayPal;
using Hub.Infrastructure.Payments.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hub.Infrastructure.Payments.Extensions;

static class PaymentsGatewayDependencyExtensions
{
    extension(IServiceCollection services)
    {
        public void AddPaymentGateways(IConfiguration configuration)
        {
            services.AddOptions<PaymentsOptions>()
                .Bind(configuration.GetSection(PaymentsOptions.SectionName));

            services.AddOptions<PayPalOptions>()
                .Bind(configuration.GetSection(PayPalOptions.SectionName));

            services.AddHttpClient(PayPalGateway.HttpClientName, (provider, client) =>
            {
                var paypal = provider.GetRequiredService<IOptions<PayPalOptions>>().Value;
                client.BaseAddress = paypal.ApiBaseUri;
                client.Timeout = TimeSpan.FromSeconds(30);
            }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                SslOptions =
                {
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                }
            });

            services.AddScoped<PayPalAccessTokenSource>();
            services.AddScoped<IPaymentGateway, PayPalGateway>();
            services.AddScoped<IPaymentGatewayResolver, PaymentGatewayResolver>();
            services.AddScoped<IPaymentWebhookParser, PayPalWebhookParser>();
            services.AddScoped<IPaymentWebhookParserResolver, PaymentWebhookParserResolver>();
        }
    }
}
