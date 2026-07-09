using Microsoft.AspNetCore.Http;

namespace Sakanak.Web.Models.ViewModels.Landlord;

public class SubmitVerificationViewModel
{
    public List<IFormFile> Documents { get; set; } = new();
}
