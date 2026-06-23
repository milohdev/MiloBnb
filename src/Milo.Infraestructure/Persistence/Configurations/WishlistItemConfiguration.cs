using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Milo.Domain.Entities;

namespace Milo.Infraestructure.Persistence.Configurations;

public sealed class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.HasKey(w => w.Id);

        builder.HasIndex(w => new { w.GuestId, w.PropertyId }).IsUnique();
        builder.HasIndex(w => w.GuestId);

        builder.HasOne(w => w.Property)
               .WithMany()
               .HasForeignKey(w => w.PropertyId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(w => w.GuestId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
