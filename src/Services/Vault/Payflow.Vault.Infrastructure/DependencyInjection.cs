using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payflow.Vault.Application.Abstractions;
using Payflow.Vault.Infrastructure.Persistence;

namespace Payflow.Vault.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddVaultInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VaultDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("VaultDb"), npgsql => npgsql.EnableRetryOnFailure()));

        services.AddScoped<IVaultTokenRepository, EfVaultTokenRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
