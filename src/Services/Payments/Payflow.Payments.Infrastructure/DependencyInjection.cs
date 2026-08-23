using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payflow.Payments.Application.Abstractions;
using Payflow.Payments.Infrastructure.Persistence;

namespace Payflow.Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Registered via the factory (not AddDbContext) so IDbContextFactory<PaymentsDbContext> can
        // also be Singleton-resolved cleanly — EfPaymentRepository.ReloadAsync uses it to read state
        // a different process/scope wrote, without the ambient request-scoped context's connection
        // potentially handing back a pinned-stale snapshot. The scoped PaymentsDbContext everything
        // else injects is created from the same factory.
        services.AddDbContextFactory<PaymentsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PaymentsDb"), npgsql => npgsql.EnableRetryOnFailure()));
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<PaymentsDbContext>>().CreateDbContext());

        services.AddScoped<IPaymentRepository, EfPaymentRepository>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
