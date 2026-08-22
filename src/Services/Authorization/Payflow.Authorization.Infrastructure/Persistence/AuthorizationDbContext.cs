using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payflow.Authorization.Domain;

namespace Payflow.Authorization.Infrastructure.Persistence;

public sealed class AuthorizationDbContext(DbContextOptions<AuthorizationDbContext> options) : DbContext(options)
{
    public DbSet<AuthorizationAttempt> AuthorizationAttempts => Set<AuthorizationAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthorizationDbContext).Assembly);
        modelBuilder.AddTransactionalOutboxEntities();
    }
}
