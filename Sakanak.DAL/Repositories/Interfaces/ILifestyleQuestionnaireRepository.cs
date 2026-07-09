using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Repositories.Interfaces;

/// <summary>
/// Provides data access operations for lifestyle questionnaire records.
/// </summary>
public interface ILifestyleQuestionnaireRepository
{
    Task<IEnumerable<LifestyleQuestionnaire>> GetAllAsync();
    Task<LifestyleQuestionnaire?> GetByIdAsync(int id);
    Task AddAsync(LifestyleQuestionnaire entity);
    Task UpdateAsync(LifestyleQuestionnaire entity);
    Task DeleteAsync(int id);
    Task<LifestyleQuestionnaire?> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<LifestyleQuestionnaire>> GetAllCompletedQuestionnairesAsync();
    Task<bool> ExistsForStudentAsync(int studentId);
}
