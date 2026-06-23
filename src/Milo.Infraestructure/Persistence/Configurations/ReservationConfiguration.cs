using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Milo.Domain.Entities;

namespace Milo.Infraestructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.TotalPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(r => r.GuestId);
        builder.HasIndex(r => r.PropertyId);
        builder.HasIndex(r => new { r.PropertyId, r.CheckInDate, r.CheckOutDate });

        builder.HasOne(r => r.Property)
               .WithMany()
               .HasForeignKey(r => r.PropertyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Guest)
               .WithMany()
               .HasForeignKey(r => r.GuestId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
