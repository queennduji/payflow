using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payflow.Notifications.Application.Abstractions;
using Payflow.Notifications.Infrastructure.Persistence;

namespace Payflow.Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("NotificationsDb"), npgsql => npgsql.EnableRetryOnFailure()));

        services.AddScoped<INotificationRepository, EfNotificationRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
