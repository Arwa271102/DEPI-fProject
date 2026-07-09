using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(e => e.PaymentId);

        builder.Property(e => e.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.DueDate)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.StripeSessionId)
            .HasMaxLength(200);

        builder.Property(e => e.StripePaymentIntentId)
            .HasMaxLength(200);

        builder.HasIndex(e => new { e.StudentId, e.Status });
        builder.HasIndex(e => new { e.LandlordId, e.Status });
        builder.HasIndex(e => e.ContractId);
        builder.HasIndex(e => e.StripeSessionId);
        builder.HasIndex(e => new { e.DueDate, e.Status });

        builder.HasOne(e => e.Student)
            .WithMany(e => e.Payments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Landlord)
            .WithMany(e => e.Payments)
            .HasForeignKey(e => e.LandlordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Apartment)
            .WithMany()
            .HasForeignKey(e => e.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Contract)
            .WithMany(e => e.Payments)
            .HasForeignKey(e => e.ContractId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
