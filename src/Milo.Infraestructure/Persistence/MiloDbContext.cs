using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Milo.Domain.Common.Interfaces;
using Milo.Domain.Entities;

namespace Milo.Infraestructure.Persistence;

public sealed class MiloDbContext(DbContextOptions<MiloDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<KycVerification> KycVerifications => Set<KycVerification>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MiloDbContext).Assembly);
        ApplySoftDeleteFilters(modelBuilder);
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;

            var method = typeof(MiloDbContext)
                .GetMethod(nameof(GetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter((LambdaExpression)method.Invoke(null, null)!);
        }
    }

    private static LambdaExpression GetSoftDeleteFilter<TEntity>() where TEntity : ISoftDeletable
    {
        Expression<Func<TEntity, bool>> filter = e => !e.IsDeleted;
        return filter;
    }
}
