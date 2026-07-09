namespace Sakanak.Domain.Entities;

public class Apartment
{
    public int ApartmentId { get; set; }
    public int LandlordId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public int TotalSeats { get; set; }
    public string[] Amenities { get; set; } = Array.Empty<string>();
    public string? VirtualTourUrl { get; set; }
    public bool IsActive { get; set; }
    public Landlord Landlord { get; set; } = null!;
    public ICollection<ApartmentGroup> ApartmentGroups { get; set; } = new List<ApartmentGroup>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<Media> Media { get; set; } = new List<Media>();
    public ICollection<Request> Requests { get; set; } = new List<Request>();
}
