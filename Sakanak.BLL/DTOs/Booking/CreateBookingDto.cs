namespace Sakanak.BLL.DTOs.Booking;

public class CreateBookingDto
{
    public int ApartmentId { get; set; }
    public DateTime RequestedStartDate { get; set; }
    public DateTime RequestedEndDate { get; set; }
    public int? ApartmentGroupId { get; set; }
    public string? Message { get; set; }
}
