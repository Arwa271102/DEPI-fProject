using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(e => e.MessageId);

        builder.Property(e => e.SenderType).HasMaxLength(30).IsRequired();
        builder.Property(e => e.RecipientType).HasMaxLength(30).IsRequired();
        builder.Property(e => e.RelatedEntityType).HasMaxLength(50);
        builder.Property(e => e.MessageText).HasMaxLength(1000).IsRequired();

        builder.HasIndex(e => new { e.RecipientUserId, e.IsRead, e.SentAt });
        builder.HasIndex(e => new { e.SenderUserId, e.RecipientUserId, e.SentAt });

        builder.HasOne(e => e.SenderUser)
            .WithMany()
            .HasForeignKey(e => e.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RecipientUser)
            .WithMany()
            .HasForeignKey(e => e.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
