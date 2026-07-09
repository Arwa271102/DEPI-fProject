using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Configurations;

public class AdminConfiguration : IEntityTypeConfiguration<Admin>
{
    public void Configure(EntityTypeBuilder<Admin> builder)
    {
        builder.ToTable("Admins");

        builder.HasKey(e => e.AdminId);

        builder.Property(e => e.ApplicationUserId)
            .IsRequired();

        builder.Property(e => e.RoleLevel)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(e => e.ApplicationUserId).IsUnique();
        builder.HasIndex(e => e.RoleLevel);

        builder.HasOne(e => e.ApplicationUser)
            .WithOne(e => e.AdminProfile)
            .HasForeignKey<Admin>(e => e.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
