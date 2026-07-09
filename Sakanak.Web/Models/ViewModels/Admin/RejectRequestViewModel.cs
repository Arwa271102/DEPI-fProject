using System.ComponentModel.DataAnnotations;

namespace Sakanak.Web.Models.ViewModels.Admin;

public class RejectRequestViewModel
{
    public int RequestId { get; set; }

    [Required]
    [MinLength(10)]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;
}
