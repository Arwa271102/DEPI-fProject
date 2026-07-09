using System.ComponentModel.DataAnnotations;

namespace Sakanak.Web.Models.ViewModels.Account;

public class ChangeEmailViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "New Email Address")]
    public string NewEmail { get; set; } = string.Empty;
}
