using Microsoft.AspNetCore.Http;

namespace Sakanak.BLL.DTOs.Contract;

public class CreateContractDto
{
    public int BookingId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public IFormFile? ContractDocument { get; set; }
    public IFormFile? IdFrontPhoto { get; set; }
    public IFormFile? IdBackPhoto { get; set; }
    public IFormFile? StudentIdCardPhoto { get; set; }
    public List<IFormFile>? SupportingDocuments { get; set; }
    public bool AcceptTerms { get; set; }
}
