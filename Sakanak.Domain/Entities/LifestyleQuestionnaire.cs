using Sakanak.Domain.Enums;

namespace Sakanak.Domain.Entities;

public class LifestyleQuestionnaire
{
    public int QuestionnaireId { get; set; }
    public int StudentId { get; set; }
    public SleepSchedule SleepSchedule { get; set; }
    public bool IsSmoker { get; set; }
    public int HygieneLevel { get; set; }
    public StudyHabits StudyHabits { get; set; }
    public SocialPreference SocialPreference { get; set; }
    public GenderPreference GenderPreference { get; set; }
    public DateTime LastUpdated { get; set; }
    public Student Student { get; set; } = null!;
}
