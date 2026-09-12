using Hub.Application.Abstractions.Messaging;
using Hub.Infrastructure.Messaging.Consumers;
using Hub.Infrastructure.Payments;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hub.Infrastructure.Messaging;

static class MassTransitDependencyExtensions
{
    public const string RabbitMqConnectionName = "rabbitmq";

    extension(IServiceCollection services)
    {
        public void AddMessaging(IConfiguration configuration)
        {
            services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();

            var connectionString = configuration.GetConnectionString(RabbitMqConnectionName);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    $"Connection string '{RabbitMqConnectionName}' is not configured.");

            services.AddMassTransit(bus =>
            {
                bus.SetKebabCaseEndpointNameFormatter();
                bus.AddConsumersFromNamespaceContaining<ProcessPaymentWebhookConsumer>();

                bus.AddEntityFrameworkOutbox<PaymentsDbContext>(outbox =>
                {
                    outbox.QueryDelay = TimeSpan.FromSeconds(1);
                    outbox.UsePostgres(enableSchemaCaching: false);
                    outbox.UseBusOutbox();
                });

                bus.AddConfigureEndpointsCallback((context, _, configurator) =>
                {
                    configurator.UseMessageRetry(retry => retry.Intervals(
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(15),
                        TimeSpan.FromSeconds(30)));
                    configurator.UseEntityFrameworkOutbox<PaymentsDbContext>(context);
                });

                bus.UsingRabbitMq((context, configurator) =>
                {
                    configurator.Host(connectionString);
                    configurator.ConfigureEndpoints(context);
                });
            });
        }
    }
}
