using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Sakanak.Web.Models.ViewModels.Landlord;

public class EditApartmentViewModel
{
    public int ApartmentId { get; set; }

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

    public List<string> Amenities { get; set; } = new();
    public string? VirtualTourUrl { get; set; }
    public List<IFormFile>? NewPhotos { get; set; } = new();
    public List<int> RemovedPhotoIds { get; set; } = new();
    public IReadOnlyList<ApartmentPhotoViewModel> ExistingPhotos { get; set; } = Array.Empty<ApartmentPhotoViewModel>();
    public IReadOnlyList<string> AvailableAmenities { get; set; } = Array.Empty<string>();
    public string LatestRequestStatus { get; set; } = string.Empty;
}
