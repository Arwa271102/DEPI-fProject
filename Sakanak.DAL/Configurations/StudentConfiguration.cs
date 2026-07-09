using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(e => e.StudentId);

        builder.Property(e => e.ApplicationUserId)
            .IsRequired();

        builder.Property(e => e.University)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Faculty)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.LatePaymentCount)
            .HasDefaultValue(0);

        builder.HasIndex(e => e.ApplicationUserId).IsUnique();
        builder.HasIndex(e => e.ApartmentGroupId);

        builder.HasOne(e => e.ApplicationUser)
            .WithOne(e => e.StudentProfile)
            .HasForeignKey<Student>(e => e.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ApartmentGroup)
            .WithMany(e => e.Students)
            .HasForeignKey(e => e.ApartmentGroupId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Media)
            .WithOne()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Questionnaire)
            .WithOne(e => e.Student)
            .HasForeignKey<LifestyleQuestionnaire>(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
