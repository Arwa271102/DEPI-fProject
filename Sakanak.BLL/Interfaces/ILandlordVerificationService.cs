using Microsoft.AspNetCore.Http;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Landlord;

namespace Sakanak.BLL.Interfaces;

public interface ILandlordVerificationService
{
    Task<Result> SubmitVerificationDocumentsAsync(int landlordId, IReadOnlyList<IFormFile> documents);
    Task<Result<LandlordVerificationDto>> GetVerificationStatusAsync(int landlordId);
}
