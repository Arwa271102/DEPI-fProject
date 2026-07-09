using Microsoft.AspNetCore.Http;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Student;

namespace Sakanak.BLL.Interfaces;

public interface IStudentProfileService
{
    Task<Result<StudentProfileDto>> GetProfileAsync(int studentId);
    Task<Result> UpdateProfileAsync(int studentId, UpdateStudentProfileDto dto);
    Task<Result<string>> UploadProfilePhotoAsync(int studentId, IFormFile photo);
    Task<Result> DeleteProfilePhotoAsync(int studentId);
    Task<Result<ProfileCompletionDto>> GetProfileCompletionAsync(int studentId);
}
