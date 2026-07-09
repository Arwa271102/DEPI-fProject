using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Configurations;

public class MediaConfiguration : IEntityTypeConfiguration<Media>
{
    public void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.HasKey(e => e.MediaId);

        builder.Property(e => e.Url)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.EntityType)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(e => new { e.EntityType, e.EntityId });
        builder.HasIndex(e => new { e.EntityType, e.EntityId, e.Type });
    }
}
