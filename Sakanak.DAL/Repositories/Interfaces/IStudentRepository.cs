using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Repositories.Interfaces;

/// <summary>
/// Provides data access operations for student records.
/// </summary>
public interface IStudentRepository
{
    Task<IEnumerable<Student>> GetAllAsync();
    Task<Student?> GetByIdAsync(int id);
    Task AddAsync(Student entity);
    Task UpdateAsync(Student entity);
    Task DeleteAsync(int id);
    Task<Student?> GetByEmailAsync(string email);
    Task<IEnumerable<Student>> GetByApartmentGroupIdAsync(int groupId);
    Task<IEnumerable<Student>> GetStudentsWithLatePaymentsAsync();
    Task<Student?> GetStudentWithQuestionnaireAsync(int studentId);
    Task<IEnumerable<Student>> GetStudentsWithoutGroupAsync();
    Task<int> GetLatePaymentCountAsync(int studentId);
    Task<Student?> GetByUserIdAsync(Guid userId);
}
