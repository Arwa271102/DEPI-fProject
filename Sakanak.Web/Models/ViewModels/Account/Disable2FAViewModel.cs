using System.ComponentModel.DataAnnotations;

namespace Sakanak.Web.Models.ViewModels.Account;

public class Disable2FAViewModel
{
    [Required]
    [Display(Name = "Authentication Code")]
    public string Code { get; set; } = string.Empty;
}
