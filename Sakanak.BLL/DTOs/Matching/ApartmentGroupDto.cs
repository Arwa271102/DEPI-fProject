namespace Sakanak.BLL.DTOs.Matching;

public class ApartmentGroupDto
{
    public int GroupId { get; set; }
    public int ApartmentId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ApartmentAddress { get; set; } = string.Empty;
    public int MaxMembers { get; set; }
    public int AvailableSeats { get; set; }
    public decimal AverageCompatibilityScore { get; set; }
    public IReadOnlyList<StudentBasicDto> Members { get; set; } = Array.Empty<StudentBasicDto>();
    public bool CanLeave { get; set; }
}
