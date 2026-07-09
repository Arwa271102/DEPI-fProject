using System.ComponentModel.DataAnnotations;

namespace Sakanak.Web.Models.ViewModels.Account;

public class VerifyCodeViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "6-Digit Code")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be exactly 6 digits.")]
    public string Code { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
