using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Milo.Application.Common.Interfaces;
using Milo.Infraestructure.Persistence;
using Milo.Infraestructure.Persistence.Interceptors;
using Milo.Infraestructure.Services;

namespace Milo.Infraestructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ICurrentUserProvider, NullCurrentUserProvider>();
        services.AddScoped<AuditInterceptor>();
        services.AddDbContext<MiloDbContext>((sp, options) =>
            options
                .UseNpgsql(configuration.GetConnectionString("Default"))
                .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
        return services;
    }
}
