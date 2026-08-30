using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Payflow.Shared.Api.Authentication;

/// <summary>
/// JWT bearer auth against Keycloak, wired identically in every service: each service validates
/// its own token rather than trusting the network path a request arrived on. The bus is the
/// internal trust boundary; every HTTP perimeter checks its own credentials.
/// </summary>
public static class AuthenticationExtensions
{
    public static WebApplicationBuilder AddPayflowAuthentication(this WebApplicationBuilder builder)
    {
        var authority = builder.Configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("Authentication:Authority is not configured.");
        var audience = builder.Configuration["Authentication:Audience"]
            ?? throw new InvalidOperationException("Authentication:Audience is not configured.");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                // Local demo runs Keycloak over plain HTTP — a real deployment never sets this.
                options.RequireHttpsMetadata = false;
            });

        builder.Services.AddAuthorization();

        return builder;
    }

    public static WebApplication UsePayflowAuthentication(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
