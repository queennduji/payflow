using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payflow.Fraud.Domain;

namespace Payflow.Fraud.Infrastructure.Persistence.Configurations;

public sealed class FraudCheckRecordConfiguration : IEntityTypeConfiguration<FraudCheckRecord>
{
    public void Configure(EntityTypeBuilder<FraudCheckRecord> builder)
    {
        builder.ToTable("fraud_check_records");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.MerchantId).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Currency).HasMaxLength(3).IsRequired();
        builder.Property(r => r.Amount).HasColumnType("numeric(18,2)");
        builder.Property(r => r.Reason).HasMaxLength(200);

        // Idempotent-consumer guarantee: one fraud decision per payment, ever.
        builder.HasIndex(r => r.PaymentId).IsUnique();

        // Velocity checks filter by merchant and a recent time window.
        builder.HasIndex(r => new { r.MerchantId, r.CreatedAt });
    }
}
