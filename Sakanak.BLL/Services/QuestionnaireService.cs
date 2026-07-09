using Microsoft.EntityFrameworkCore;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Questionnaire;
using Sakanak.BLL.Interfaces;
using Sakanak.DAL.Data;
using Sakanak.Domain.Entities;

namespace Sakanak.BLL.Services;

public class QuestionnaireService : IQuestionnaireService
{
    private readonly SakanakDbContext _dbContext;

    public QuestionnaireService(SakanakDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<LifestyleQuestionnaireDto>> GetQuestionnaireAsync(int studentId)
    {
        var questionnaire = await _dbContext.LifestyleQuestionnaires
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.StudentId == studentId);

        if (questionnaire is null)
        {
            return Result<LifestyleQuestionnaireDto>.Success(new LifestyleQuestionnaireDto());
        }

        return Result<LifestyleQuestionnaireDto>.Success(Map(questionnaire));
    }

    public async Task<Result> SaveQuestionnaireAsync(int studentId, LifestyleQuestionnaireDto dto)
    {
        if (dto.HygieneLevel is < 1 or > 5)
        {
            return Result.Failure("Hygiene level must be between 1 and 5.");
        }

        var student = await _dbContext.Students
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.StudentId == studentId);
        if (student is null)
        {
            return Result.Failure("Student profile was not found.");
        }

        var questionnaire = await _dbContext.LifestyleQuestionnaires
            .FirstOrDefaultAsync(item => item.StudentId == studentId);

        if (questionnaire is null)
        {
            questionnaire = new LifestyleQuestionnaire
            {
                StudentId = studentId
            };
            _dbContext.LifestyleQuestionnaires.Add(questionnaire);
        }

        questionnaire.SleepSchedule = dto.SleepSchedule;
        questionnaire.IsSmoker = dto.IsSmoker;
        questionnaire.HygieneLevel = dto.HygieneLevel;
        questionnaire.StudyHabits = dto.StudyHabits;
        questionnaire.SocialPreference = dto.SocialPreference;
        questionnaire.GenderPreference = dto.GenderPreference;
        questionnaire.LastUpdated = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(student.University) && !string.IsNullOrWhiteSpace(student.Faculty))
        {
            student.ApplicationUser.IsProfileComplete = true;
        }

        await _dbContext.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<bool>> IsQuestionnaireCompleteAsync(int studentId)
    {
        var exists = await _dbContext.LifestyleQuestionnaires.AnyAsync(item => item.StudentId == studentId);
        return Result<bool>.Success(exists);
    }

    private static LifestyleQuestionnaireDto Map(LifestyleQuestionnaire questionnaire)
        => new()
        {
            QuestionnaireId = questionnaire.QuestionnaireId,
            SleepSchedule = questionnaire.SleepSchedule,
            IsSmoker = questionnaire.IsSmoker,
            HygieneLevel = questionnaire.HygieneLevel,
            StudyHabits = questionnaire.StudyHabits,
            SocialPreference = questionnaire.SocialPreference,
            GenderPreference = questionnaire.GenderPreference,
            LastUpdated = questionnaire.LastUpdated
        };
}
