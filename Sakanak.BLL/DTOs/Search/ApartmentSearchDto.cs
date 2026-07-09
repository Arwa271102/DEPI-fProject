namespace Sakanak.BLL.DTOs.Search;

public class ApartmentSearchDto
{
    public string? City { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinSeats { get; set; }
    public int? MaxSeats { get; set; }
    public List<string> Amenities { get; set; } = new();
    public DateTime? AvailableFrom { get; set; }
    public string SortBy { get; set; } = "CreatedDate";
    public bool Descending { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
