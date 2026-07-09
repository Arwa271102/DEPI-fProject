using System.ComponentModel.DataAnnotations;

namespace Sakanak.Web.Models.ViewModels.Account;

public class UpdateProfileViewModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Range(15, 100)]
    public int Age { get; set; }

    public string? Email { get; set; } // Read-only for display in the profile section
}
