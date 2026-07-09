using System.ComponentModel.DataAnnotations;

namespace Sakanak.Web.Models.ViewModels.Account;

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;
}
