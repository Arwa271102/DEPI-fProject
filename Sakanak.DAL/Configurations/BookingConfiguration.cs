using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        var amenitiesComparer = new ValueComparer<string[]>(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            value => value.ToArray());

        builder.HasKey(e => e.BookingId);

        builder.Property(e => e.BookingDate)
            .IsRequired();

        builder.Property(e => e.RequestedStartDate)
            .IsRequired();

        builder.Property(e => e.RequestedEndDate)
            .IsRequired();

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(500);

        builder.Property(e => e.CancellationReason)
            .HasMaxLength(500);

        builder.Property(e => e.Message)
            .HasMaxLength(1000);

        builder.Property(e => e.PricePerMonthAtBooking)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.AddressAtBooking)
            .HasMaxLength(300);

        builder.Property(e => e.CityAtBooking)
            .HasMaxLength(100);

        builder.Property(e => e.AmenitiesAtBooking)
            .HasConversion(
                value => string.Join(";", value),
                value => value.Split(';', StringSplitOptions.RemoveEmptyEntries))
            .Metadata.SetValueComparer(amenitiesComparer);

        builder.Property(e => e.AmenitiesAtBooking)
            .HasMaxLength(1000);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(e => new { e.StudentId, e.Status });
        builder.HasIndex(e => new { e.ApartmentId, e.Status });
        builder.HasIndex(e => e.ApartmentGroupId);

        builder.HasOne(e => e.Student)
            .WithMany(e => e.Bookings)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Apartment)
            .WithMany(e => e.Bookings)
            .HasForeignKey(e => e.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApartmentGroup)
            .WithMany(e => e.Bookings)
            .HasForeignKey(e => e.ApartmentGroupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
