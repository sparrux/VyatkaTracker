using FluentValidation;

namespace Hub.Application.Features.Payments.Commands.CreateDonation;

sealed class CreateDonationCommandValidator : AbstractValidator<CreateDonationCommand>
{
    public CreateDonationCommandValidator()
    {
        RuleFor(x => x.Request.Amount)
            .GreaterThan(0)
            .LessThan(1_000_000_000);

        RuleFor(x => x.Request.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency must be a 3-letter ISO 4217 code");

        RuleFor(x => x.Request.Description)
            .MaximumLength(127);

        RuleFor(x => x.Request.Provider)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Provider));

        RuleFor(x => x.Request.EventId)
            .NotEqual(Guid.Empty)
            .When(x => x.Request.EventId.HasValue);

        RuleFor(x => x.Request.ReturnUrl)
            .Must(BeAbsoluteUri)
            .When(x => x.Request.ReturnUrl is not null)
            .WithMessage("Return URL must be an absolute URI");

        RuleFor(x => x.Request.CancelUrl)
            .Must(BeAbsoluteUri)
            .When(x => x.Request.CancelUrl is not null)
            .WithMessage("Cancel URL must be an absolute URI");

        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.IdempotencyKey));

        RuleFor(x => x.Request.IdempotencyKey)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.IdempotencyKey));
    }

    static bool BeAbsoluteUri(Uri? uri) =>
        uri is { IsAbsoluteUri: true };
}
