using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payflow.Authorization.Application.Abstractions;
using Payflow.Authorization.Infrastructure.Persistence;

namespace Payflow.Authorization.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthorizationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuthorizationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("AuthorizationDb")));

        services.AddScoped<IAuthorizationStore, EfAuthorizationStore>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
