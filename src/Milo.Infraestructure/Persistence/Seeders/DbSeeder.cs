using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Milo.Domain.Entities;
using Milo.Domain.Entities.Enums;

namespace Milo.Infraestructure.Persistence.Seeders;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var context = services.GetRequiredService<MiloDbContext>();

        if (await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
            return;

        var email = configuration["Admin:Email"]!;
        var password = configuration["Admin:Password"]!;

        var hasher = new PasswordHasher<User>();
        var temp = User.Create("Admin", "MiloBnb", email, string.Empty, UserRole.Admin);
        var hash = hasher.HashPassword(temp, password);
        var admin = User.Create("Admin", "MiloBnb", email, hash, UserRole.Admin);

        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}
