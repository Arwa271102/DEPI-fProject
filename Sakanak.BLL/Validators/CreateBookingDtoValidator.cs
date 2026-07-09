using FluentValidation;
using Microsoft.Extensions.Options;
using Sakanak.BLL.DTOs.Booking;
using Sakanak.BLL.Options;

namespace Sakanak.BLL.Validators;

public class CreateBookingDtoValidator : AbstractValidator<CreateBookingDto>
{
    public CreateBookingDtoValidator(IOptions<BusinessRuleOptions> businessRules)
    {
        var minimumRentalDays = businessRules.Value.MinimumRentalDays;
        RuleFor(x => x.ApartmentId).GreaterThan(0);
        RuleFor(x => x.RequestedStartDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Start date must be today or in the future.");
        RuleFor(x => x.RequestedEndDate).NotEmpty();
        RuleFor(x => x.RequestedEndDate)
            .GreaterThan(x => x.RequestedStartDate)
            .WithMessage("End date must be after the start date.");
        RuleFor(x => x.RequestedEndDate)
            .Must((dto, endDate) => (endDate.Date - dto.RequestedStartDate.Date).TotalDays >= minimumRentalDays)
            .WithMessage($"Minimum rental period is {minimumRentalDays} days.");
        RuleFor(x => x.Message).MaximumLength(1000);
    }
}
