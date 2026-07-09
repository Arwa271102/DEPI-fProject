using Sakanak.BLL.DTOs.Search;

namespace Sakanak.Web.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<ApartmentListItemDto> FeaturedApartments { get; set; } = new();
    }
}
