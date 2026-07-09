using Sakanak.Domain.Enums;

namespace Sakanak.Domain.Entities;

public class Admin
{
    public int AdminId { get; set; }
    public Guid ApplicationUserId { get; set; }
    public AdminRoleLevel RoleLevel { get; set; }
    public ApplicationUser ApplicationUser { get; set; } = null!;
    public ICollection<Contract> VerifiedContracts { get; set; } = new List<Contract>();
    public ICollection<Request> ReviewedRequests { get; set; } = new List<Request>();
}
