using FluentValidation;
using Microsoft.Extensions.Options;
using Sakanak.BLL.DTOs.Contract;
using Sakanak.BLL.Options;

namespace Sakanak.BLL.Validators;

public class CreateContractDtoValidator : AbstractValidator<CreateContractDto>
{
    public CreateContractDtoValidator(IOptions<BusinessRuleOptions> businessRules)
    {
        var minimumRentalDays = businessRules.Value.MinimumRentalDays;
        RuleFor(x => x.BookingId).GreaterThan(0);
        RuleFor(x => x.StartDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Start date must be today or in the future.");
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after the start date.");
        RuleFor(x => x.EndDate)
            .Must((dto, endDate) => (endDate.Date - dto.StartDate.Date).TotalDays >= minimumRentalDays)
            .WithMessage($"Minimum rental period is {minimumRentalDays} days.");
        RuleFor(x => x.ContractDocument).NotNull().WithMessage("Contract PDF is required.");
        RuleFor(x => x.IdFrontPhoto).NotNull().WithMessage("ID front photo is required.");
        RuleFor(x => x.IdBackPhoto).NotNull().WithMessage("ID back photo is required.");
        RuleFor(x => x.AcceptTerms).Equal(true).WithMessage("You must accept the contract submission terms.");
    }
}
