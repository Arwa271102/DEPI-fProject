using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Configurations;

public class ApartmentConfiguration : IEntityTypeConfiguration<Apartment>
{
    private static readonly ValueComparer<string[]> AmenitiesComparer = new(
        (left, right) => (left ?? Array.Empty<string>()).SequenceEqual(right ?? Array.Empty<string>()),
        value => (value ?? Array.Empty<string>()).Aggregate(0, (current, item) => HashCode.Combine(current, item.GetHashCode())),
        value => (value ?? Array.Empty<string>()).ToArray());

    public void Configure(EntityTypeBuilder<Apartment> builder)
    {
        builder.HasKey(e => e.ApartmentId);

        builder.Property(e => e.Address)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(e => e.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PricePerMonth)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.TotalSeats)
            .IsRequired();

        builder.Property(e => e.VirtualTourUrl)
            .HasMaxLength(500);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(e => e.Amenities)
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => JsonSerializer.Deserialize<string[]>(value, (JsonSerializerOptions?)null) ?? Array.Empty<string>())
            .Metadata.SetValueComparer(AmenitiesComparer);
        
        builder.Property(e => e.Amenities)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.HasIndex(e => e.LandlordId);
        builder.HasIndex(e => new { e.City, e.IsActive });
        builder.HasIndex(e => new { e.PricePerMonth, e.IsActive });

        builder.HasOne(e => e.Landlord)
            .WithMany(e => e.Apartments)
            .HasForeignKey(e => e.LandlordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Media)
            .WithOne()
            .HasForeignKey(e => e.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
