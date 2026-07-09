using System.ComponentModel.DataAnnotations;

namespace Sakanak.Web.Models.ViewModels.Account;

public class RegisterViewModel
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [Required]
    [Range(16, 120)]
    public int Age { get; set; }

    [StringLength(200)]
    public string? University { get; set; }

    [StringLength(200)]
    public string? Faculty { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
