namespace Sakanak.BLL.DTOs.Contract;

public class ContractDto
{
    public int ContractId { get; set; }
    public int BookingId { get; set; }
    public string ApartmentAddress { get; set; } = string.Empty;
    public string ApartmentCity { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentUniversity { get; set; } = string.Empty;
    public string LandlordName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DocumentUrl { get; set; }
    public string? RejectionReason { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancellationDate { get; set; }
    public string? CancelledByName { get; set; }
    public bool CanResubmit { get; set; }
}
