using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payflow.Payments.Domain;

namespace Payflow.Payments.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.MerchantId).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Key).HasMaxLength(200).IsRequired();
        builder.Property(r => r.ResponseBody).IsRequired();

        // The actual dedup guarantee: a concurrent duplicate insert fails at the database, not in
        // application code racing on a prior SELECT.
        builder.HasIndex(r => new { r.MerchantId, r.Key }).IsUnique();
    }
}
