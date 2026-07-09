using System.ComponentModel.DataAnnotations;

namespace Sakanak.Web.Models.ViewModels.Account;

public class CompleteProfileViewModel
{
    [DataType(DataType.Password)]
    [Display(Name = "Create Password")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }

    [Required(ErrorMessage = "University is required.")]
    public string University { get; set; } = string.Empty;

    [Required(ErrorMessage = "Faculty is required.")]
    public string Faculty { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Invalid phone number.")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Age is required.")]
    [Range(16, 100, ErrorMessage = "Age must be between 16 and 100.")]
    public int Age { get; set; }

    public string? Role { get; set; }

    public bool NeedsPassword { get; set; }
}
