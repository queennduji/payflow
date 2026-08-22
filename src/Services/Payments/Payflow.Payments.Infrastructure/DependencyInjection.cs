using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payflow.Payments.Application.Abstractions;
using Payflow.Payments.Infrastructure.Clients;
using Payflow.Payments.Infrastructure.Persistence;

namespace Payflow.Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PaymentsDb")));

        services.AddScoped<IPaymentRepository, EfPaymentRepository>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddHttpClient<IAuthorizationClient, HttpAuthorizationClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:AuthorizationBaseUrl"]
                ?? throw new InvalidOperationException("Services:AuthorizationBaseUrl is not configured."));
        });

        services.AddHttpClient<ILedgerClient, HttpLedgerClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:LedgerBaseUrl"]
                ?? throw new InvalidOperationException("Services:LedgerBaseUrl is not configured."));
        });

        return services;
    }
}
