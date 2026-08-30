using Microsoft.EntityFrameworkCore;
using Payflow.Vault.Domain;

namespace Payflow.Vault.Infrastructure.Persistence;

public sealed class VaultDbContext(DbContextOptions<VaultDbContext> options) : DbContext(options)
{
    public DbSet<VaultToken> VaultTokens => Set<VaultToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VaultDbContext).Assembly);
}
