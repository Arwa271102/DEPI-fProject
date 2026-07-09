using Sakanak.BLL.DTOs.Search;

namespace Sakanak.Web.Models.ViewModels.Student;

public class ApartmentDiscoveryDetailsViewModel
{
    public ApartmentDetailDto Apartment { get; set; } = new();
    public List<Sakanak.BLL.DTOs.Matching.RoommateMatchDto> CurrentRoommates { get; set; } = new();
}
