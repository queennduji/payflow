using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payflow.Ledger.Domain;

namespace Payflow.Ledger.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasMaxLength(200);
        builder.Property(a => a.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Currency).HasMaxLength(3).IsRequired();

        builder.Ignore(a => a.DomainEvents);
    }
}
