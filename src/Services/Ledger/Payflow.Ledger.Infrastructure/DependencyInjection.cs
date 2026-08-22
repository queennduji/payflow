using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payflow.Ledger.Application.Abstractions;
using Payflow.Ledger.Infrastructure.Persistence;

namespace Payflow.Ledger.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLedgerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LedgerDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("LedgerDb")));

        services.AddScoped<ILedgerEntryGroupRepository, EfLedgerEntryGroupRepository>();
        services.AddScoped<IAccountRepository, EfAccountRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
