using FluentValidation;

namespace Hub.Application.Features.Payments.Commands.ReceivePaymentWebhook;

sealed class ReceivePaymentWebhookCommandValidator : AbstractValidator<ReceivePaymentWebhookCommand>
{
    public ReceivePaymentWebhookCommandValidator()
    {
        RuleFor(command => command.Provider)
            .NotEmpty();

        RuleFor(command => command.Payload)
            .NotEmpty();
    }
}
