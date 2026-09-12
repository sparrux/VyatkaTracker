using Hub.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hub.Infrastructure.Payments.Configurations;

sealed class PaymentWebhookEventConfiguration : IEntityTypeConfiguration<PaymentWebhookEvent>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookEvent> builder)
    {
        builder.ToPaymentsTable("payment_webhook_event");

        builder.ConfigurePaymentAggregate();
        builder.ConfigureProviderName(webhook => webhook.Provider);

        builder.Property(webhook => webhook.ProviderEventId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(webhook => webhook.EventType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(webhook => webhook.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(webhook => webhook.ProviderPaymentId)
            .HasMaxLength(200);

        builder.Property(webhook => webhook.Status).AsStringEnum();

        builder.Property(webhook => webhook.FailureReason)
            .HasMaxLength(1000);

        builder.HasIndex(webhook => webhook.Status);

        builder.HasIndex(webhook => new { webhook.Provider, webhook.ProviderEventId })
            .IsUnique();
    }
}
