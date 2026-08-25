using Microsoft.Extensions.DependencyInjection;
using Payflow.Payments.Application.Observability;

namespace Payflow.Payments.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddSingleton<PaymentMetrics>();
        return services;
    }
}
