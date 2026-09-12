using FluentValidation;

namespace Hub.Application.Features.Payments.Commands.RecordCashDonation;

sealed class RecordCashDonationCommandValidator : AbstractValidator<RecordCashDonationCommand>
{
    public RecordCashDonationCommandValidator()
    {
        RuleFor(x => x.Request.UserId)
            .NotEmpty();

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

        RuleFor(x => x.Request.EventId)
            .NotEqual(Guid.Empty)
            .When(x => x.Request.EventId.HasValue);

        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.IdempotencyKey));

        RuleFor(x => x.Request.IdempotencyKey)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.IdempotencyKey));
    }
}
