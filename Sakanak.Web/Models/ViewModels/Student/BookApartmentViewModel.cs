using System.ComponentModel.DataAnnotations;
using Sakanak.BLL.DTOs.Search;

namespace Sakanak.Web.Models.ViewModels.Student;

public class BookApartmentViewModel
{
    public ApartmentDetailDto Apartment { get; set; } = new();
    public int ApartmentId { get; set; }

    [DataType(DataType.Date)]
    public DateTime RequestedStartDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);

    [DataType(DataType.Date)]
    public DateTime RequestedEndDate { get; set; } = DateTime.UtcNow.Date.AddMonths(3).AddDays(7);

    [StringLength(1000)]
    public string? Message { get; set; }
}
