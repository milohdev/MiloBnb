using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Milo.Domain.Entities;

namespace Milo.Infraestructure.Persistence.Configurations;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000).IsRequired();
        builder.Property(p => p.Address).HasMaxLength(200).IsRequired();
        builder.Property(p => p.City).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Country).HasMaxLength(200).IsRequired();
        builder.Property(p => p.PricePerNight).HasPrecision(18, 2).IsRequired();

        builder.HasIndex(p => p.City);
        builder.HasIndex(p => p.OwnerId);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(p => p.OwnerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Images)
               .WithOne()
               .HasForeignKey(i => i.PropertyId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
