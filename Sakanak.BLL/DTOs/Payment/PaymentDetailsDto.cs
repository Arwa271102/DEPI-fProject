namespace Sakanak.BLL.DTOs.Payment;

public class PaymentDetailsDto : PaymentDto
{
    public string ApartmentCity { get; set; } = string.Empty;
    public DateTime ContractStartDate { get; set; }
    public DateTime ContractEndDate { get; set; }
    public decimal MonthlyRate { get; set; }
    public int BillingMonths { get; set; }
    public string? StripeSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }
}
