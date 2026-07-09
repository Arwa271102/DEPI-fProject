namespace Sakanak.BLL.DTOs.Matching;

public class GroupSuggestionDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string ApartmentAddress { get; set; } = string.Empty;
    public IReadOnlyList<StudentBasicDto> Members { get; set; } = Array.Empty<StudentBasicDto>();
    public int AvailableSeats { get; set; }
    public decimal AverageCompatibilityScore { get; set; }
    public bool CanJoin { get; set; }
}
