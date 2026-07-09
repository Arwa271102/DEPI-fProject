namespace Sakanak.BLL.DTOs.Apartment;

public class ApartmentOccupancyDto
{
    public int ApartmentId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string LandlordName { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public int TotalSeats { get; set; }
    public int OccupiedSeats { get; set; }
    public int AvailableSeats { get; set; }
    public decimal OccupancyRate { get; set; }
    public bool IsActive { get; set; }
    public string? PrimaryPhotoUrl { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public IReadOnlyList<RoommateDto> Tenants { get; set; } = Array.Empty<RoommateDto>();
}
