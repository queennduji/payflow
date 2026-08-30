using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payflow.Vault.Domain;

namespace Payflow.Vault.Infrastructure.Persistence.Configurations;

public sealed class VaultTokenConfiguration : IEntityTypeConfiguration<VaultToken>
{
    public void Configure(EntityTypeBuilder<VaultToken> builder)
    {
        builder.ToTable("vault_tokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token).HasMaxLength(64).IsRequired();
        builder.Property(t => t.Last4).HasMaxLength(4).IsRequired();
        builder.HasIndex(t => t.Token).IsUnique();
    }
}
