using FluentValidation;
using Microsoft.Extensions.Options;
using Sakanak.BLL.DTOs.Request;
using Sakanak.BLL.Options;

namespace Sakanak.BLL.Validators;

public class AdminReviewRequestDtoValidator : AbstractValidator<AdminReviewRequestDto>
{
    public AdminReviewRequestDtoValidator(IOptions<BusinessRuleOptions> businessRules)
    {
        RuleFor(x => x.RequestId).GreaterThan(0);

        When(_ => businessRules.Value.RequireRejectionReason, () =>
        {
            RuleFor(x => x.Reason)
                .NotEmpty()
                .MinimumLength(10)
                .MaximumLength(2000);
        });
    }
}
