namespace Sakanak.BLL.Options;

public class BusinessRuleOptions
{
    public const string SectionName = "BusinessRules";

    public int MinimumPhotosRequired { get; set; } = 1;
    public int MaxApartmentSeats { get; set; } = 20;
    public bool RequireRejectionReason { get; set; } = true;
    public int MinimumRentalDays { get; set; } = 90;
    public int ContractExpiryCheckIntervalHours { get; set; } = 24;
}
