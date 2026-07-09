namespace Sakanak.BLL.DTOs.Apartment;

public class StudentApartmentAssignmentDto
{
    public bool HasAssignment { get; set; }
    public int ApartmentId { get; set; }
    public int ContractId { get; set; }
    public int? PaymentId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal MonthlyRent { get; set; }
    public int TotalSeats { get; set; }
    public int OccupiedSeats { get; set; }
    public int AvailableSeats { get; set; }
    public decimal OccupancyRate { get; set; }
    public string[] Amenities { get; set; } = Array.Empty<string>();
    public string? VirtualTourUrl { get; set; }
    public IReadOnlyList<string> PhotoUrls { get; set; } = Array.Empty<string>();
    public string LandlordName { get; set; } = string.Empty;
    public string LandlordEmail { get; set; } = string.Empty;
    public string? LandlordPhoneNumber { get; set; }
    public Guid LandlordApplicationUserId { get; set; }
    public DateTime ContractStartDate { get; set; }
    public DateTime ContractEndDate { get; set; }
    public int DaysUntilMoveIn { get; set; }
    public int DaysRemaining { get; set; }
    public IReadOnlyList<RoommateDto> Roommates { get; set; } = Array.Empty<RoommateDto>();
}
