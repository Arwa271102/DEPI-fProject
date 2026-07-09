using System.ComponentModel.DataAnnotations;

namespace Sakanak.BLL.DTOs.Profile;

public class UpdateProfileDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Range(15, 100)]
    public int Age { get; set; }
}
