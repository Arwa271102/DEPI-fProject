using System.ComponentModel.DataAnnotations;

namespace Sakanak.BLL.DTOs.Request;

public class AdminReviewRequestDto
{
    [Required]
    public int RequestId { get; set; }

    [StringLength(2000)]
    public string? Reason { get; set; }
}
