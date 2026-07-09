namespace Sakanak.BLL.DTOs.Contract;

public class ContractDetailsDto : ContractDto
{
    public string StudentEmail { get; set; } = string.Empty;
    public string? StudentPhone { get; set; }
    public string StudentFaculty { get; set; } = string.Empty;
    public string LandlordEmail { get; set; } = string.Empty;
    public string? LandlordPhone { get; set; }
    public decimal PricePerMonth { get; set; }
    public DateTime BookingStartDate { get; set; }
    public DateTime BookingEndDate { get; set; }
    public string? ReviewedByAdminName { get; set; }
    public IReadOnlyList<ContractDocumentDto> Documents { get; set; } = Array.Empty<ContractDocumentDto>();
}
