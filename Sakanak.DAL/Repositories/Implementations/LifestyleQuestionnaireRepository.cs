using Microsoft.EntityFrameworkCore;
using Sakanak.DAL.Data;
using Sakanak.DAL.Repositories.Interfaces;
using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Repositories.Implementations;

public class LifestyleQuestionnaireRepository : RepositoryBase<LifestyleQuestionnaire>, ILifestyleQuestionnaireRepository
{
    public LifestyleQuestionnaireRepository(SakanakDbContext context) : base(context)
    {
    }

    public async Task<LifestyleQuestionnaire?> GetByStudentIdAsync(int studentId)
        => await DbSet.FirstOrDefaultAsync(e => e.StudentId == studentId);

    public async Task<IEnumerable<LifestyleQuestionnaire>> GetAllCompletedQuestionnairesAsync()
        => await DbSet.ToListAsync();

    public async Task<bool> ExistsForStudentAsync(int studentId)
        => await DbSet.AnyAsync(e => e.StudentId == studentId);
}
