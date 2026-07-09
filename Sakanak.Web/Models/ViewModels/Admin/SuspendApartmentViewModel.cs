using System.ComponentModel.DataAnnotations;

namespace Sakanak.Web.Models.ViewModels.Admin;

public class SuspendApartmentViewModel
{
    public int ApartmentId { get; set; }

    [Required]
    [MinLength(10)]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;
}
