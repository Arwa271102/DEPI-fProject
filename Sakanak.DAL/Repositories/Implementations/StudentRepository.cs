using Microsoft.EntityFrameworkCore;
using Sakanak.DAL.Data;
using Sakanak.DAL.Repositories.Interfaces;
using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Repositories.Implementations;

public class StudentRepository : RepositoryBase<Student>, IStudentRepository
{
    public StudentRepository(SakanakDbContext context) : base(context)
    {
    }

    public async Task<Student?> GetByEmailAsync(string email)
        => await DbSet
            .Include(e => e.ApplicationUser)
            .FirstOrDefaultAsync(e => e.ApplicationUser.Email == email);

    public async Task<IEnumerable<Student>> GetByApartmentGroupIdAsync(int groupId)
        => await DbSet.Where(e => e.ApartmentGroupId == groupId).ToListAsync();

    public async Task<IEnumerable<Student>> GetStudentsWithLatePaymentsAsync()
        => await DbSet.Where(e => e.LatePaymentCount > 0).ToListAsync();

    public async Task<Student?> GetStudentWithQuestionnaireAsync(int studentId)
        => await DbSet.Include(e => e.Questionnaire).FirstOrDefaultAsync(e => e.StudentId == studentId);

    public async Task<IEnumerable<Student>> GetStudentsWithoutGroupAsync()
        => await DbSet.Where(e => e.ApartmentGroupId == null).ToListAsync();

    public async Task<int> GetLatePaymentCountAsync(int studentId)
        => await DbSet.Where(e => e.StudentId == studentId).Select(e => e.LatePaymentCount).FirstOrDefaultAsync();

    public async Task<Student?> GetByUserIdAsync(Guid userId)
        => await DbSet.FirstOrDefaultAsync(e => e.ApplicationUserId == userId);
}
