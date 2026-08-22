using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payflow.Ledger.Domain;

namespace Payflow.Ledger.Infrastructure.Persistence.Configurations;

public sealed class LedgerEntryGroupConfiguration : IEntityTypeConfiguration<LedgerEntryGroup>
{
    public void Configure(EntityTypeBuilder<LedgerEntryGroup> builder)
    {
        builder.ToTable("ledger_entry_groups");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Currency).HasMaxLength(3).IsRequired();

        // One posting per payment, enforced at the database: this is what makes ledger posting
        // safe to retry from Payments (see PostLedgerEntryCommandHandler).
        builder.HasIndex(g => g.PaymentId).IsUnique();

        builder.OwnsMany(g => g.Lines, lines =>
        {
            lines.ToTable("ledger_lines");
            lines.WithOwner().HasForeignKey("LedgerEntryGroupId");
            lines.Property<long>("Id");
            lines.HasKey("Id");

            lines.Property(l => l.AccountId).HasMaxLength(200).IsRequired();
            lines.Property(l => l.Direction).HasConversion<string>().HasMaxLength(10);

            lines.OwnsOne(l => l.Amount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
                money.Property(m => m.Currency).HasColumnName("currency").HasMaxLength(3);
            });

            lines.Navigation(l => l.Amount).IsRequired();
            lines.HasIndex(l => l.AccountId); // balance lookups filter by account
        });
        builder.Navigation(g => g.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(g => g.DomainEvents);
    }
}
