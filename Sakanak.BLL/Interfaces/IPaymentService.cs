using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.DTOs.Payment;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Interfaces;

public interface IPaymentService
{
    Task<Result<int>> GeneratePaymentForContractAsync(int contractId);
    Task<Result<PaymentDto>> GetPaymentForContractAsync(int contractId, int studentId);
    Task<Result<string>> CreateCheckoutSessionAsync(int paymentId, int studentId, string successUrl, string cancelUrl);
    Task<Result<PaymentDetailsDto>> VerifyAndCompletePaymentAsync(string sessionId);
    Task<Result<List<PaymentDto>>> GetStudentPaymentsAsync(int studentId);
    Task<Result<List<PaymentDto>>> GetLandlordPaymentsAsync(int landlordId);
    Task<Result<PaymentDetailsDto>> GetPaymentDetailsAsync(int paymentId, int userId);
    Task<Result<PaymentDetailsDto>> GetPaymentDetailsForAdminAsync(int paymentId);
    Task<Result<PagedResult<PaymentDto>>> GetAllPaymentsAsync(int page, int pageSize, PaymentStatus? statusFilter = null);
}
