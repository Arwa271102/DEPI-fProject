using System.ComponentModel.DataAnnotations;

namespace Sakanak.Web.Models.ViewModels.Admin;

public class SuspendLandlordViewModel
{
    public int LandlordId { get; set; }

    [Required]
    [MinLength(10)]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;
}
