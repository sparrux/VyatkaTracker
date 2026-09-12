using Hub.Application.Abstractions.Messaging;
using Hub.Application.Features.Payments.Mapping;
using Hub.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Hub.Infrastructure.Messaging;

sealed class DomainEventOutboxInterceptor(
    IServiceProvider services
) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var aggregates = eventData.Context.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        if (aggregates.Count == 0)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var publisher = services.GetRequiredService<IIntegrationEventPublisher>();

        foreach (var aggregate in aggregates)
        {
            foreach (var message in PaymentIntegrationEvents.From(aggregate.DomainEvents))
                await publisher.Publish(message, cancellationToken);

            aggregate.ClearDomainEvents();
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
