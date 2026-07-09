using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Sakanak.BLL.DTOs.Booking;

namespace Sakanak.Web.Models.ViewModels.Student;

public class CreateContractViewModel
{
    public int BookingId { get; set; }
    public int? ContractId { get; set; }
    public BookingDetailsDto? Booking { get; set; }

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    public IFormFile? ContractDocument { get; set; }
    public IFormFile? IdFrontPhoto { get; set; }
    public IFormFile? IdBackPhoto { get; set; }
    public IFormFile? StudentIdCardPhoto { get; set; }
    public List<IFormFile>? SupportingDocuments { get; set; }

    [Display(Name = "I confirm this contract is accurate and ready for admin review.")]
    public bool AcceptTerms { get; set; }
}
