using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payflow.Notifications.Domain;

namespace Payflow.Notifications.Infrastructure.Persistence.Configurations;

public sealed class NotificationAttemptConfiguration : IEntityTypeConfiguration<NotificationAttempt>
{
    public void Configure(EntityTypeBuilder<NotificationAttempt> builder)
    {
        builder.ToTable("notification_attempts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.MerchantId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Status).HasMaxLength(20).IsRequired();

        builder.HasIndex(a => a.PaymentId).IsUnique();
        builder.HasIndex(a => a.MerchantId);
    }
}
