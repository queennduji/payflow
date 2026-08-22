using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payflow.Notifications.Domain;

namespace Payflow.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<NotificationAttempt> NotificationAttempts => Set<NotificationAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
        modelBuilder.AddTransactionalOutboxEntities();
    }
}
