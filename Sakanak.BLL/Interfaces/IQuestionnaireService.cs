using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Questionnaire;

namespace Sakanak.BLL.Interfaces;

public interface IQuestionnaireService
{
    Task<Result<LifestyleQuestionnaireDto>> GetQuestionnaireAsync(int studentId);
    Task<Result> SaveQuestionnaireAsync(int studentId, LifestyleQuestionnaireDto dto);
    Task<Result<bool>> IsQuestionnaireCompleteAsync(int studentId);
}
