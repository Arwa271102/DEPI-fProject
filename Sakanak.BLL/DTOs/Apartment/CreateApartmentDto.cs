using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Sakanak.BLL.DTOs.Apartment;

public class CreateApartmentDto
{
    [Required]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal PricePerMonth { get; set; }

    [Range(1, 20)]
    public int TotalSeats { get; set; }

    public List<string> Amenities { get; set; } = new();

    [Url]
    [StringLength(500)]
    public string? VirtualTourUrl { get; set; }

    public List<IFormFile> Photos { get; set; } = new();
}
