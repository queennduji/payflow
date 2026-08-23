using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payflow.Fraud.Application.Abstractions;
using Payflow.Fraud.Infrastructure.Persistence;

namespace Payflow.Fraud.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFraudInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FraudDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("FraudDb"), npgsql => npgsql.EnableRetryOnFailure()));

        services.AddScoped<IFraudCheckRepository, EfFraudCheckRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
