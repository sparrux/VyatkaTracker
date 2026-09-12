using FluentValidation;

namespace Hub.Application.Features.Payments.Commands.CancelCashDonation;

sealed class CancelCashDonationCommandValidator : AbstractValidator<CancelCashDonationCommand>
{
    public CancelCashDonationCommandValidator()
    {
        RuleFor(x => x.DonationId)
            .NotEmpty();
    }
}
