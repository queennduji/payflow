using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payflow.Payments.Application.Saga;

namespace Payflow.Payments.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the saga instance like any other entity – MassTransit's EF Core saga repository works
/// against a plain <c>DbSet&lt;TSaga&gt;</c> on an existing <see cref="PaymentsDbContext"/>, no
/// special base class required.
/// </summary>
public sealed class PaymentSagaStateConfiguration : IEntityTypeConfiguration<PaymentSagaState>
{
    public void Configure(EntityTypeBuilder<PaymentSagaState> builder)
    {
        builder.ToTable("payment_saga_state");
        builder.HasKey(s => s.CorrelationId);

        builder.Property(s => s.CurrentState).HasMaxLength(50).IsRequired();
        builder.Property(s => s.MerchantId).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Currency).HasMaxLength(3).IsRequired();
        builder.Property(s => s.PaymentMethodRef).HasMaxLength(200).IsRequired();
        builder.Property(s => s.PendingFailureReason).HasMaxLength(200);
        builder.Property(s => s.Amount).HasColumnType("numeric(18,2)");
        builder.Property(s => s.ResponseAddress).HasConversion(
            uri => uri == null ? null : uri.ToString(),
            value => value == null ? null : new Uri(value));
    }
}
