using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Configurations;

public class LandlordConfiguration : IEntityTypeConfiguration<Landlord>
{
    public void Configure(EntityTypeBuilder<Landlord> builder)
    {
        builder.ToTable("Landlords");

        builder.HasKey(e => e.LandlordId);

        builder.Property(e => e.ApplicationUserId)
            .IsRequired();

        builder.Property(e => e.VerificationStatus)
            .IsRequired();

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(2000);

        builder.Property(e => e.TotalProperties)
            .HasDefaultValue(0);

        builder.HasIndex(e => e.ApplicationUserId).IsUnique();
        builder.HasIndex(e => e.VerificationStatus);
        builder.HasIndex(e => e.VerificationRequestedAt);

        builder.HasOne(e => e.ApplicationUser)
            .WithOne(e => e.LandlordProfile)
            .HasForeignKey<Landlord>(e => e.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Media)
            .WithOne()
            .HasForeignKey(e => e.LandlordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.VerifiedByAdmin)
            .WithMany()
            .HasForeignKey(e => e.VerifiedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
