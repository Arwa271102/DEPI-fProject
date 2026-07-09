using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.DTOs.Contract;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Interfaces;

public interface IContractService
{
    Task<Result<int>> CreateContractAsync(int studentId, CreateContractDto dto);
    Task<Result<List<ContractDto>>> GetStudentContractsAsync(int studentId, ContractStatus? statusFilter = null);
    Task<Result<ContractDetailsDto>> GetContractDetailsAsync(int contractId, int studentId);
    Task<Result> UpdateContractAsync(int contractId, int studentId, CreateContractDto dto);
    Task<Result<PagedResult<ContractDto>>> GetPendingContractsAsync(int pageNumber = 1, int pageSize = 10);
    Task<Result<PagedResult<ContractDto>>> GetContractHistoryAsync(int pageNumber = 1, int pageSize = 10);
    Task<Result<ContractDetailsDto>> GetContractDetailsForAdminAsync(int contractId);
    Task<Result> ApproveContractAsync(int contractId, Guid adminApplicationUserId);
    Task<Result> RejectContractAsync(int contractId, Guid adminApplicationUserId, string reason);
    Task<Result<List<ContractDto>>> GetLandlordContractsAsync(int landlordId, int? apartmentId = null);
    Task<Result<ContractDetailsDto>> GetContractDetailsForLandlordAsync(int contractId, int landlordId);
    Task<Result<bool>> CanCreateContractForBookingAsync(int bookingId, int studentId);
    Task<Result> CancelContractAsync(int contractId, int studentId);
    Task<Result> AdminCancelContractAsync(int contractId, Guid adminApplicationUserId, string reason);
}
