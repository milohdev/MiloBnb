using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Milo.Domain.Entities;

namespace Milo.Infraestructure.Persistence.Configurations;

public sealed class KycVerificationConfiguration : IEntityTypeConfiguration<KycVerification>
{
    public void Configure(EntityTypeBuilder<KycVerification> builder)
    {
        builder.HasKey(k => k.Id);

        builder.HasIndex(k => k.UserId);

        builder.Property(k => k.Status)
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(k => k.ExtractedFirstName).HasMaxLength(200);
        builder.Property(k => k.ExtractedLastName).HasMaxLength(200);
        builder.Property(k => k.ExtractedDocumentNumber).HasMaxLength(100);
        builder.Property(k => k.RejectionReason).HasMaxLength(1000);
        builder.Property(k => k.DocumentImageUrl).HasMaxLength(2048);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(k => k.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
