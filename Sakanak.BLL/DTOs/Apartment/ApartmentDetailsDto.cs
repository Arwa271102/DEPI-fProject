namespace Sakanak.BLL.DTOs.Apartment;

public class ApartmentDetailsDto
{
    public int ApartmentId { get; set; }
    public int LandlordId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public int TotalSeats { get; set; }
    public int OccupiedSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int ActiveBookingCount { get; set; }
    public List<string> Amenities { get; set; } = new();
    public string? VirtualTourUrl { get; set; }
    public bool IsActive { get; set; }
    public string LatestRequestStatus { get; set; } = "No Request";
    public string? LatestRequestMessage { get; set; }
    public DateTime? LatestRequestDate { get; set; }
    public DateTime? LatestResolvedAt { get; set; }
    public List<ApartmentMediaDto> Photos { get; set; } = new();
}
