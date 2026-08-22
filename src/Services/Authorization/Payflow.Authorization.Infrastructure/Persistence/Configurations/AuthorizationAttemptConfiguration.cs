using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payflow.Authorization.Domain;

namespace Payflow.Authorization.Infrastructure.Persistence.Configurations;

public sealed class AuthorizationAttemptConfiguration : IEntityTypeConfiguration<AuthorizationAttempt>
{
    public void Configure(EntityTypeBuilder<AuthorizationAttempt> builder)
    {
        builder.ToTable("authorization_attempts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.DeclineReason).HasMaxLength(200);
        builder.Property(a => a.ProcessorReference).HasMaxLength(50).IsRequired();

        // Idempotent-consumer guarantee: one authorization decision per payment, ever — this is
        // what closes the Phase-1 gap ADR-0002 flagged about Authorization's in-memory store.
        builder.HasIndex(a => a.PaymentId).IsUnique();
    }
}
