using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payflow.Authorization.Application.Abstractions;
using Payflow.Authorization.Infrastructure.Persistence;
using Polly;

namespace Payflow.Authorization.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthorizationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuthorizationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("AuthorizationDb"), npgsql => npgsql.EnableRetryOnFailure()));

        services.AddScoped<IAuthorizationStore, EfAuthorizationStore>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.Configure<ChaosOptions>(configuration.GetSection("Chaos"));
        services.AddSingleton<SimulatedCardNetworkClient>();
        services.AddSingleton<ICardNetworkClient, ResilientCardNetworkClient>();
        services.AddResiliencePipeline(ResilientCardNetworkClient.PipelineKey, builder => CardNetworkResiliencePipeline.Configure(builder));

        return services;
    }
}
