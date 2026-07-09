using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(e => e.NotificationId);

        builder.Property(e => e.Title).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Message).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.ActionUrl).HasMaxLength(500);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(80).IsRequired();

        builder.HasIndex(e => new { e.RecipientUserId, e.IsRead, e.CreatedAt });

        builder.HasOne(e => e.RecipientUser)
            .WithMany()
            .HasForeignKey(e => e.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
