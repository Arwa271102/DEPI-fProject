using Sakanak.BLL.DTOs.Payment;

namespace Sakanak.BLL.Interfaces;

public interface IStripeService
{
    Task<StripeCheckoutSessionDto> CreateCheckoutSessionAsync(int paymentId, decimal amount, string studentEmail, string description, string successUrl, string cancelUrl);
    Task<bool> VerifySessionPaidAsync(string sessionId);
    Task<string?> GetPaymentIntentIdAsync(string sessionId);
}
