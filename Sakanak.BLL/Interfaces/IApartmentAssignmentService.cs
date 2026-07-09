using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Apartment;

namespace Sakanak.BLL.Interfaces;

public interface IApartmentAssignmentService
{
    Task<Result<StudentApartmentAssignmentDto>> GetStudentCurrentApartmentAsync(int studentId);
    Task<Result<List<ApartmentOccupancyDto>>> GetLandlordApartmentsWithOccupancyAsync(int landlordId);
    Task<Result<ApartmentOccupancyDto>> GetApartmentTenantsAsync(int apartmentId, int landlordId);
    Task<Result<List<ApartmentOccupancyDto>>> GetAllApartmentsWithOccupancyAsync();
}
