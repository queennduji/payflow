using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payflow.Payments.Domain;

namespace Payflow.Payments.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.MerchantId).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PaymentMethodRef).HasMaxLength(200).IsRequired();
        builder.Property(p => p.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.FailureReason).HasMaxLength(500);

        builder.OwnsOne(p => p.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
            money.Property(m => m.Currency).HasColumnName("currency").HasMaxLength(3);
        });
        builder.Navigation(p => p.Amount).IsRequired();

        // One payment per (merchant, idempotency key) — belt-and-suspenders alongside the
        // dedicated IdempotencyRecord table, which is the actual dedup fast-path.
        builder.HasIndex(p => new { p.MerchantId, p.IdempotencyKey }).IsUnique();

        // Domain events are an in-memory notification mechanism, not persisted state.
        builder.Ignore(p => p.DomainEvents);
    }
}
