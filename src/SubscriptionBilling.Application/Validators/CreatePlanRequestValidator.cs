using FluentValidation;
using SubscriptionBilling.Application.DTOs.Plans;

namespace SubscriptionBilling.Application.Validators;

public class CreatePlanRequestValidator : AbstractValidator<CreatePlanRequest>
{
    public CreatePlanRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Plan name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code (e.g. 'usd').");

        RuleFor(x => x.Features)
            .NotEmpty().WithMessage("At least one plan feature must be provided.");
    }
}
