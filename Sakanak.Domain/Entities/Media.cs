using Sakanak.Domain.Enums;

namespace Sakanak.Domain.Entities;

public class Media
{
    public int MediaId { get; set; }
    public string Url { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }

    // Explicit foreign keys for navigation properties
    public int? ApartmentId { get; set; }
    public int? ContractId { get; set; }
    public int? LandlordId { get; set; }
    public int? StudentId { get; set; }
}
