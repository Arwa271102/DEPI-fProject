using System.ComponentModel.DataAnnotations;

namespace Sakanak.Web.Models.ViewModels.Account;

public class ExternalRegisterViewModel
{
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name must be between 2 and 100 characters.", MinimumLength = 2)]
    [Display(Name = "Full Name")]
    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(30, ErrorMessage = "Username must be between 3 and 30 characters.", MinimumLength = 3)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Invalid phone number.")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Age is required.")]
    [Range(16, 100, ErrorMessage = "Age must be between 16 and 100.")]
    public int Age { get; set; }

    [Display(Name = "University")]
    public string? University { get; set; }

    [Display(Name = "Faculty")]
    public string? Faculty { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
