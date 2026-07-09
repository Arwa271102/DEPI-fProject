using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.DAL.Configurations;

public class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.HasKey(e => e.RequestId);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasDefaultValue(RequestStatus.Pending);

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(RequestType.ApartmentUpload)
            .IsRequired();

        builder.Property(e => e.Message)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(e => e.PreviousValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => new { e.LandlordId, e.Status });
        builder.HasIndex(e => new { e.ApartmentId, e.Status });
        builder.HasIndex(e => e.ReviewedByAdminId);

        builder.HasOne(e => e.Landlord)
            .WithMany(e => e.Requests)
            .HasForeignKey(e => e.LandlordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Apartment)
            .WithMany(e => e.Requests)
            .HasForeignKey(e => e.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReviewedByAdmin)
            .WithMany(e => e.ReviewedRequests)
            .HasForeignKey(e => e.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
