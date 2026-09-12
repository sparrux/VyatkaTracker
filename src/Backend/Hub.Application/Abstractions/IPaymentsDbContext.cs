using Hub.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Hub.Application.Abstractions;

public interface IPaymentsDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<PaymentMethod> PaymentMethods { get; }

    DbSet<Payment> Payments { get; }
    DbSet<PaymentAttempt> PaymentAttempts { get; }
    DbSet<Refund> Refunds { get; }

    DbSet<Donation> Donations { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<Invoice> Invoices { get; }

    DbSet<Product> Products { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }

    DbSet<PaymentWebhookEvent> WebhookEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    void RejectChanges();

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
