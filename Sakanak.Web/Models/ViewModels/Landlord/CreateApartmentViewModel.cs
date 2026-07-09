using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Sakanak.Web.Models.ViewModels.Landlord;

public class CreateApartmentViewModel
{
    [Required]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999")]
    [Display(Name = "Price Per Month")]
    public decimal PricePerMonth { get; set; }

    [Range(1, 20)]
    [Display(Name = "Total Seats")]
    public int TotalSeats { get; set; }

    [Display(Name = "Amenities")]
    public List<string> Amenities { get; set; } = new();

    [Url]
    [Display(Name = "Virtual Tour URL")]
    public string? VirtualTourUrl { get; set; }

    [Display(Name = "Apartment Photos")]
    public List<IFormFile> Photos { get; set; } = new();

    public IReadOnlyList<string> AvailableAmenities { get; set; } = Array.Empty<string>();
}
