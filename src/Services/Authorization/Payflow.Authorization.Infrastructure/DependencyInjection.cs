using Microsoft.Extensions.DependencyInjection;
using Payflow.Authorization.Application.Abstractions;

namespace Payflow.Authorization.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthorizationInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationStore, InMemoryAuthorizationStore>();
        return services;
    }
}
