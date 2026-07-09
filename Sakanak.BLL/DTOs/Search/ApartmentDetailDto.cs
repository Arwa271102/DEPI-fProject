using Sakanak.BLL.DTOs.Apartment;

namespace Sakanak.BLL.DTOs.Search;

public class ApartmentDetailDto
{
    public int ApartmentId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public IReadOnlyList<string> Amenities { get; set; } = Array.Empty<string>();
    public string? VirtualTourUrl { get; set; }
    public IReadOnlyList<ApartmentMediaDto> Photos { get; set; } = Array.Empty<ApartmentMediaDto>();
    public Guid LandlordApplicationUserId { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public string? LandlordPhoneNumber { get; set; }
    public bool LandlordVerified { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
