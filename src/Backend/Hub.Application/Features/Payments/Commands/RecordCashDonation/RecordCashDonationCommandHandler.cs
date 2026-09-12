using Ardalis.Result;
using Hub.Application.Abstractions.Payments;
using Hub.Application.Features.Payments.Commands.ConfirmCashDonation;
using Hub.Application.Features.Payments.Commands.CreateDonation;
using Hub.Application.Features.Payments.Contracts;
using Hub.Application.Pipelines;
using Hub.Domain.Payments;

namespace Hub.Application.Features.Payments.Commands.RecordCashDonation;

sealed class RecordCashDonationCommandHandler(
    IRequestHandler<CreateDonationCommand, DonationResponse> createDonation,
    IRequestHandler<ConfirmCashDonationCommand, DonationResponse> confirmCashDonation
) : IRequestHandler<RecordCashDonationCommand, DonationResponse>
{
    public async Task<Result<DonationResponse>> Handle(
        RecordCashDonationCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        var created = await createDonation.Handle(
            new CreateDonationCommand(
                request.UserId,
                new CreateDonationRequest(
                    request.Amount,
                    request.Currency,
                    request.IsAnonymous,
                    request.EventId,
                    PaymentGatewayNames.Cash,
                    request.Description,
                    ReturnUrl: null,
                    CancelUrl: null,
                    request.IdempotencyKey),
                command.IdempotencyKey),
            cancellationToken);

        if (!created.IsSuccess)
            return created;

        if (created.Value.Status is DonationStatus.Completed)
            return created;

        var confirmed = await confirmCashDonation.Handle(
            new ConfirmCashDonationCommand(created.Value.Id),
            cancellationToken);

        if (!confirmed.IsSuccess)
            return confirmed;

        return Result.Created(confirmed.Value);
    }
}
