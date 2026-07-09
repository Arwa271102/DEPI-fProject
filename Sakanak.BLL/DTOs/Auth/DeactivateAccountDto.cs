using System.ComponentModel.DataAnnotations;

namespace Sakanak.BLL.DTOs.Auth;

public class DeactivateAccountDto
{
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
