namespace Sakanak.BLL.DTOs.Booking;

public class BookingDto
{
    public int BookingId { get; set; }
    public int ApartmentId { get; set; }
    public string ApartmentAddress { get; set; } = string.Empty;
    public string ApartmentCity { get; set; } = string.Empty;
    public string? ApartmentPhotoUrl { get; set; }
    public decimal PricePerMonth { get; set; }
    public decimal CurrentPricePerMonth { get; set; }
    public IReadOnlyList<string> AmenitiesAtBooking { get; set; } = Array.Empty<string>();
    public string StudentName { get; set; } = string.Empty;
    public string? StudentPhotoUrl { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public DateTime RequestedStartDate { get; set; }
    public DateTime RequestedEndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public string? CancellationReason { get; set; }
    public int DaysUntilStart { get; set; }
}
