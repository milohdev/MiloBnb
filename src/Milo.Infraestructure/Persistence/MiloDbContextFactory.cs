using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Milo.Infraestructure.Persistence.Interceptors;
using Milo.Infraestructure.Services;

namespace Milo.Infraestructure.Persistence;

public sealed class MiloDbContextFactory : IDesignTimeDbContextFactory<MiloDbContext>
{
    public MiloDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MiloDbContext>()
            .UseNpgsql("Host=localhost;Database=milobnb;Username=postgres;Password=postgres")
            .AddInterceptors(new AuditInterceptor(new NullCurrentUserProvider()))
            .Options;
        return new MiloDbContext(options);
    }
}
