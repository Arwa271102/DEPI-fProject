using FluentValidation;
using Microsoft.Extensions.Configuration;
using Sakanak.BLL.DTOs.Apartment;

namespace Sakanak.BLL.Validators;

public class CreateApartmentDtoValidator : AbstractValidator<CreateApartmentDto>
{
    public CreateApartmentDtoValidator(IConfiguration configuration)
    {
        var allowedAmenities = GetAllowedAmenities(configuration);

        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PricePerMonth).GreaterThan(0);
        RuleFor(x => x.TotalSeats).InclusiveBetween(1, 20);
        RuleForEach(x => x.Amenities).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Amenities)
            .Must(amenities => HaveOnlyConfiguredAmenities(amenities, allowedAmenities))
            .WithMessage("One or more selected amenities are not supported.");
        RuleFor(x => x.Photos).NotNull().Must(files => files.Count >= 1)
            .WithMessage("At least one apartment photo is required.");
        RuleFor(x => x.VirtualTourUrl)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("Virtual tour URL must be a valid absolute URL.");
    }

    private static HashSet<string> GetAllowedAmenities(IConfiguration configuration)
    {
        var configuredAmenities = configuration.GetSection("Amenities").Get<string[]>() ?? Array.Empty<string>();
        return configuredAmenities
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool HaveOnlyConfiguredAmenities(IEnumerable<string>? amenities, HashSet<string> allowedAmenities)
    {
        if (allowedAmenities.Count == 0)
        {
            return true;
        }

        return amenities == null || amenities
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .All(value => allowedAmenities.Contains(value.Trim()));
    }
}
