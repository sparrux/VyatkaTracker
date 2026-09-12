using Hub.Infrastructure.Hangfire.Extensions;
using Hub.Infrastructure.Messaging;
using Hub.Infrastructure.Payments.Extensions;
using Hub.Infrastructure.Persistence.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hub.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(
        this IServiceCollection services,
        string dbConnectionName,
        IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddPersistenceServices(dbConnectionName, configuration);
        services.AddSchedulerServices(dbConnectionName, configuration);
        services.AddPaymentGateways(configuration);
        services.AddMessaging(configuration);
    }
}
