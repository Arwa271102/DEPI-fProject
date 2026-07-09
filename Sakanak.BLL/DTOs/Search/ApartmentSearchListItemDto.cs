namespace Sakanak.BLL.DTOs.Search;

public class ApartmentSearchListItemDto
{
    public int ApartmentId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public int TotalSeats { get; set; }
    public string? PrimaryPhotoUrl { get; set; }
    public IReadOnlyList<string> Amenities { get; set; } = Array.Empty<string>();
    public string LandlordName { get; set; } = string.Empty;
}
