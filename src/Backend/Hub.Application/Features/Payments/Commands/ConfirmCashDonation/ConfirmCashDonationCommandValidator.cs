using FluentValidation;

namespace Hub.Application.Features.Payments.Commands.ConfirmCashDonation;

sealed class ConfirmCashDonationCommandValidator : AbstractValidator<ConfirmCashDonationCommand>
{
    public ConfirmCashDonationCommandValidator()
    {
        RuleFor(x => x.DonationId)
            .NotEmpty();
    }
}
