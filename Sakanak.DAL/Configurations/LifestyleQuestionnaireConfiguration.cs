using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Configurations;

public class LifestyleQuestionnaireConfiguration : IEntityTypeConfiguration<LifestyleQuestionnaire>
{
    public void Configure(EntityTypeBuilder<LifestyleQuestionnaire> builder)
    {
        builder.HasKey(e => e.QuestionnaireId);

        builder.Property(e => e.SleepSchedule)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.StudyHabits)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.SocialPreference)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.GenderPreference)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.HygieneLevel)
            .IsRequired();

        builder.Property(e => e.LastUpdated)
            .IsRequired();

        builder.HasIndex(e => e.StudentId).IsUnique();
    }
}
