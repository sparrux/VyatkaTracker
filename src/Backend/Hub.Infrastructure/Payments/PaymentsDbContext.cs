using Hub.Application.Abstractions;
using Hub.Domain.Payments;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Hub.Infrastructure.Payments;

public sealed class PaymentsDbContext : DbContext, IPaymentsDbContext
{
    public const string Schema = "payments";

    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options)
    {
        Customers = Set<Customer>();
        PaymentMethods = Set<PaymentMethod>();

        Payments = Set<Payment>();
        PaymentAttempts = Set<PaymentAttempt>();
        Refunds = Set<Refund>();

        Donations = Set<Donation>();
        SubscriptionPlans = Set<SubscriptionPlan>();
        Subscriptions = Set<Subscription>();
        Invoices = Set<Invoice>();

        Products = Set<Product>();
        Orders = Set<Order>();
        OrderItems = Set<OrderItem>();

        WebhookEvents = Set<PaymentWebhookEvent>();
    }

    public DbSet<Customer> Customers { get; }
    public DbSet<PaymentMethod> PaymentMethods { get; }

    public DbSet<Payment> Payments { get; }
    public DbSet<PaymentAttempt> PaymentAttempts { get; }
    public DbSet<Refund> Refunds { get; }

    public DbSet<Donation> Donations { get; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    public DbSet<Subscription> Subscriptions { get; }
    public DbSet<Invoice> Invoices { get; }

    public DbSet<Product> Products { get; }
    public DbSet<Order> Orders { get; }
    public DbSet<OrderItem> OrderItems { get; }

    public DbSet<PaymentWebhookEvent> WebhookEvents { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PaymentsDbContext).Assembly,
            type => type.Namespace?.StartsWith("Hub.Infrastructure.Payments", StringComparison.Ordinal) == true);

        modelBuilder.AddInboxStateEntity(entity => entity.ToTable("inbox_state", Schema));
        modelBuilder.AddOutboxMessageEntity(entity => entity.ToTable("outbox_message", Schema));
        modelBuilder.AddOutboxStateEntity(entity => entity.ToTable("outbox_state", Schema));
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Database.BeginTransactionAsync(cancellationToken);

    public void RejectChanges() => ChangeTracker.Clear();
}
