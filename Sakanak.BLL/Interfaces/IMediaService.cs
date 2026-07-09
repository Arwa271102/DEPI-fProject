using Microsoft.AspNetCore.Http;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Apartment;

namespace Sakanak.BLL.Interfaces;

public interface IMediaService
{
    Task<Result<IReadOnlyList<ApartmentMediaDto>>> UploadApartmentPhotosAsync(int apartmentId, IEnumerable<IFormFile> files);
    Task<IReadOnlyList<ApartmentMediaDto>> GetApartmentMediaAsync(int apartmentId);
    Task<Result> DeleteMediaAsync(int mediaId);
    Task<Result> DeleteApartmentMediaAsync(int apartmentId);
    Result ValidateApartmentFiles(IEnumerable<IFormFile> files);
}
