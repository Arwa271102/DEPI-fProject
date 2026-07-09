namespace Sakanak.BLL.DTOs.Booking;

public class BookingDetailsDto : BookingDto
{
    public int StudentId { get; set; }
    public Guid StudentApplicationUserId { get; set; }
    public string StudentEmail { get; set; } = string.Empty;
    public string? StudentPhone { get; set; }
    public string StudentUniversity { get; set; } = string.Empty;
    public string StudentFaculty { get; set; } = string.Empty;
    public int StudentAge { get; set; }
    public string? Message { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public IReadOnlyList<string> Amenities { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> CurrentAmenities { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> PhotoUrls { get; set; } = Array.Empty<string>();
    public string? SleepSchedule { get; set; }
    public bool? IsSmoker { get; set; }
    public int? HygieneLevel { get; set; }
    public string? StudyHabits { get; set; }
    public string? SocialPreference { get; set; }
    public string? GenderPreference { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool HasActiveContract { get; set; }
    public bool IsInApartmentGroup { get; set; }
}
