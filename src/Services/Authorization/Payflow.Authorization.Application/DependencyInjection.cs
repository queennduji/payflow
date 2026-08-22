using Microsoft.Extensions.DependencyInjection;

namespace Payflow.Authorization.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthorizationApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}
