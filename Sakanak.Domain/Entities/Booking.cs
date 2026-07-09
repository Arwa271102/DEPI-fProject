using Sakanak.Domain.Enums;

namespace Sakanak.Domain.Entities;

public class Booking
{
    public int BookingId { get; set; }
    public int StudentId { get; set; }
    public int ApartmentId { get; set; }
    public int? ApartmentGroupId { get; set; }
    public DateTime BookingDate { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime RequestedStartDate { get; set; }
    public DateTime RequestedEndDate { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? CancellationReason { get; set; }
    public string? Message { get; set; }
    public decimal PricePerMonthAtBooking { get; set; }
    public string AddressAtBooking { get; set; } = string.Empty;
    public string CityAtBooking { get; set; } = string.Empty;
    public string[] AmenitiesAtBooking { get; set; } = Array.Empty<string>();
    public Student Student { get; set; } = null!;
    public Apartment Apartment { get; set; } = null!;
    public ApartmentGroup? ApartmentGroup { get; set; }
    public Contract? Contract { get; set; }
}
