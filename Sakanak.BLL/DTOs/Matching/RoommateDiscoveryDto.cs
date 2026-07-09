namespace Sakanak.BLL.DTOs.Matching;

public class RoommateDiscoveryDto
{
    // Student Info
    public int StudentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string University { get; set; } = string.Empty;
    public string Faculty { get; set; } = string.Empty;
    public string? ProfilePhotoUrl { get; set; }
    
    // Compatibility
    public decimal CompatibilityScore { get; set; } // 0-100
    public List<string> CompatibilityReasons { get; set; } = new();
    
    // Status
    public StudentHousingStatus HousingStatus { get; set; }
    
    // If HasApartment
    public int? ApartmentId { get; set; }
    public string? ApartmentAddress { get; set; }
    public string? ApartmentCity { get; set; }
    public decimal? ApartmentPrice { get; set; }
    public int? AvailableSeats { get; set; }
}

public enum StudentHousingStatus
{
    Searching,      // No apartment yet
    HasApartment    // Has active booking or active contract
}
