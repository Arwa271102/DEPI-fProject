using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Matching;

namespace Sakanak.BLL.Interfaces;

public interface IMatchingService
{
    Task<Result<decimal>> CalculateCompatibilityAsync(int studentAId, int studentBId);
    Task<Result<List<RoommateMatchDto>>> GetCompatibleStudentsForApartmentAsync(int studentId, int apartmentId);
    Task<Result<ApartmentGroupDto>> GetMyGroupAsync(int studentId);
    Task<Result> LeaveGroupAsync(int groupId, int studentId);
    Task<Result<List<RoommateMatchDto>>> GetApartmentGroupMembersAsync(int apartmentId);
    Task<Result<List<RoommateMatchDto>>> GetGroupCompatibilityForStudentAsync(int studentId, int apartmentId);
    Task<Result<List<RoommateDiscoveryDto>>> FindCompatibleStudentsAsync(int studentId);
}
