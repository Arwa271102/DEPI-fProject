using FluentValidation;
using Sakanak.BLL.DTOs.Student;

namespace Sakanak.BLL.Validators;

public class UpdateStudentProfileDtoValidator : AbstractValidator<UpdateStudentProfileDto>
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png"];

    public UpdateStudentProfileDtoValidator()
    {
        RuleFor(dto => dto.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(dto => dto.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(dto => dto.PhoneNumber)
            .MaximumLength(30)
            .When(dto => !string.IsNullOrWhiteSpace(dto.PhoneNumber));

        RuleFor(dto => dto.University)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(dto => dto.Faculty)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(dto => dto.ProfilePhoto)
            .Must(file => file is null || file.Length <= 5 * 1024 * 1024)
            .WithMessage("Profile photo must be 5MB or smaller.")
            .Must(file => file is null || AllowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            .WithMessage("Profile photo must be a JPG or PNG image.");
    }
}
