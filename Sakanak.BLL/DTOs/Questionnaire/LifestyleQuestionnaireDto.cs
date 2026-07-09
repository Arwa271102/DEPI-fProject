using Sakanak.Domain.Enums;

namespace Sakanak.BLL.DTOs.Questionnaire;

public class LifestyleQuestionnaireDto
{
    public int? QuestionnaireId { get; set; }
    public SleepSchedule SleepSchedule { get; set; } = SleepSchedule.Flexible;
    public bool IsSmoker { get; set; }
    public int HygieneLevel { get; set; } = 3;
    public StudyHabits StudyHabits { get; set; } = StudyHabits.Flexible;
    public SocialPreference SocialPreference { get; set; } = SocialPreference.Moderate;
    public GenderPreference GenderPreference { get; set; } = GenderPreference.NoPreference;
    public DateTime? LastUpdated { get; set; }
}
