using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Milo.Application.Common.Interfaces;
using Milo.Domain.Repositories;
using Milo.Infraestructure.Persistence;
using Milo.Infraestructure.Persistence.Interceptors;
using Milo.Infraestructure.Persistence.Repositories;
using Milo.Infraestructure.Services;


namespace Milo.Infraestructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserProvider, HttpContextCurrentUserProvider>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IPropertyImageRepository, PropertyImageRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IKycRepository, KycRepository>();
        services.AddHttpClient<IKycService, ClaudeKycService>();
        services.AddScoped<AuditInterceptor>();
        services.AddDbContext<MiloDbContext>((sp, options) =>
            options
                .UseNpgsql(configuration.GetConnectionString("Default"))
                .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
        return services;
    }
}
