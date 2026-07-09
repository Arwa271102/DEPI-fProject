namespace Sakanak.BLL.DTOs.Payment;

public class PaymentDto
{
    public int PaymentId { get; set; }
    public int ContractId { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsLate { get; set; }
    public bool CanPay { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string ApartmentAddress { get; set; } = string.Empty;
    public string LandlordName { get; set; } = string.Empty;
}
