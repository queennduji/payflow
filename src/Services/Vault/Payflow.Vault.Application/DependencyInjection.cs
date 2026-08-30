using Microsoft.Extensions.DependencyInjection;

namespace Payflow.Vault.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddVaultApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}
