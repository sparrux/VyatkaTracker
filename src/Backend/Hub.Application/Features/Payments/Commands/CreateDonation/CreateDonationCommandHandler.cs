using Ardalis.Result;
using Hub.Application.Abstractions;
using Hub.Application.Abstractions.Payments;
using Hub.Application.Features.Payments.Contracts;
using Hub.Application.Pipelines;
using Hub.Domain.Payments;
using Hub.Domain.Payments.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Hub.Application.Features.Payments.Commands.CreateDonation;

sealed class CreateDonationCommandHandler(
    IHubDbContext hubDbContext,
    IPaymentsDbContext paymentsDbContext,
    IPaymentGatewayResolver gatewayResolver
) : IRequestHandler<CreateDonationCommand, DonationResponse>
{
    public async Task<Result<DonationResponse>> Handle(
        CreateDonationCommand command,
        CancellationToken cancellationToken)
    {
        var userExists = await hubDbContext.Users
            .AnyAsync(user => user.Id == command.UserId, cancellationToken);
        if (!userExists)
            return Result.NotFound("User not found");

        var request = command.Request;
        var idempotencyKey = Normalize(command.IdempotencyKey) ?? Normalize(request.IdempotencyKey);

        if (idempotencyKey is not null)
        {
            var existing = await FindByIdempotencyKey(idempotencyKey, cancellationToken);
            if (existing is { } found)
            {
                if (found.Payment.CustomerId != command.UserId)
                    return Result.Conflict("Idempotency key is already used");

                return await ResumeCheckout(found.Donation, found.Payment, request, cancellationToken);
            }
        }

        if (request.EventId is { } eventId)
        {
            var eventExists = await hubDbContext.Events
                .AnyAsync(ev => ev.Id == eventId, cancellationToken);
            if (!eventExists)
                return Result.NotFound("Event not found by id");
        }

        var amount = Money.Create(request.Amount, request.Currency);
        if (!amount.IsSuccess)
            return amount.Map();

        var customer = await GetOrCreateCustomer(command.UserId, cancellationToken);
        if (!customer.IsSuccess)
            return customer.Map();

        BusinessReference? reference = null;
        if (request.EventId is { } contributionEventId)
        {
            var createdReference = BusinessReference.ForEventContribution(contributionEventId);
            if (!createdReference.IsSuccess)
                return createdReference.Map();

            reference = createdReference.Value;
        }

        var donation = Donation.Create(amount.Value, customer.Value.Id, request.IsAnonymous, reference);
        if (!donation.IsSuccess)
            return donation.Map();

        var payment = Payment.Create(
            amount.Value,
            PaymentPurpose.Donation,
            donation.Value.Id,
            customer.Value.Id,
            idempotencyKey);
        if (!payment.IsSuccess)
            return payment.Map();

        var attached = donation.Value.AttachPayment(payment.Value.Id);
        if (!attached.IsSuccess)
            return attached.Map();

        await paymentsDbContext.Donations.AddAsync(donation.Value, cancellationToken);
        await paymentsDbContext.Payments.AddAsync(payment.Value, cancellationToken);
        await paymentsDbContext.SaveChangesAsync(cancellationToken);

        return await StartCheckout(donation.Value, payment.Value, request, cancellationToken);
    }

    async Task<Result<DonationResponse>> StartCheckout(
        Donation donation,
        Payment payment,
        CreateDonationRequest request,
        CancellationToken cancellationToken)
    {
        var gateway = gatewayResolver.Resolve(request.Provider);
        if (!gateway.IsSuccess)
            return gateway.Map();

        var provider = ProviderName.Create(gateway.Value.Name);
        if (!provider.IsSuccess)
            return provider.Map();

        var attempt = payment.StartAttempt(provider.Value);
        if (!attempt.IsSuccess)
            return attempt.Map();

        await paymentsDbContext.SaveChangesAsync(cancellationToken);

        return await CheckoutWithGateway(
            donation,
            payment,
            attempt.Value,
            gateway.Value,
            request,
            cancellationToken);
    }

    async Task<Result<DonationResponse>> ResumeCheckout(
        Donation donation,
        Payment payment,
        CreateDonationRequest request,
        CancellationToken cancellationToken)
    {
        if (donation.Status is DonationStatus.Completed or DonationStatus.Cancelled)
            return Result.Success(DonationResponse.From(donation, payment));

        if (payment.Status is PaymentStatus.Succeeded or PaymentStatus.Cancelled)
            return Result.Success(DonationResponse.From(donation, payment));

        var gateway = gatewayResolver.Resolve(request.Provider);
        if (!gateway.IsSuccess)
            return gateway.Map();

        var attempt = payment.Attempts
            .OrderByDescending(x => x.AttemptNumber)
            .FirstOrDefault();

        if (attempt is null || attempt.Status is PaymentAttemptStatus.Failed or PaymentAttemptStatus.Cancelled)
        {
            var provider = ProviderName.Create(gateway.Value.Name);
            if (!provider.IsSuccess)
                return provider.Map();

            var started = payment.StartAttempt(provider.Value, attempt?.ProviderPaymentId);
            if (!started.IsSuccess)
                return started.Map();

            attempt = started.Value;
            await paymentsDbContext.SaveChangesAsync(cancellationToken);
        }

        var resumed = await CheckoutWithGateway(
            donation,
            payment,
            attempt,
            gateway.Value,
            request,
            cancellationToken);

        return resumed.IsSuccess
            ? Result.Success(resumed.Value)
            : resumed;
    }

    async Task<Result<DonationResponse>> CheckoutWithGateway(
        Donation donation,
        Payment payment,
        PaymentAttempt attempt,
        IPaymentGateway gateway,
        CreateDonationRequest request,
        CancellationToken cancellationToken)
    {
        var created = await gateway.CreatePaymentAsync(
            new CreateGatewayPaymentRequest(
                payment.Amount,
                donation.Id.ToString(),
                Normalize(request.Description) ?? "Donation",
                DonationCheckout.GatewayIdempotencyKey(attempt, "create"),
                request.ReturnUrl,
                request.CancelUrl),
            cancellationToken);

        if (!created.IsSuccess)
        {
            payment.FailAttempt(attempt.Id, null, created.Errors.FirstOrDefault());
            await paymentsDbContext.SaveChangesAsync(cancellationToken);
            return created.Map();
        }

        var applied = DonationCheckout.ApplyProviderResult(
            payment,
            donation,
            attempt.Id,
            created.Value.ProviderPaymentId,
            created.Value.Status);

        if (!applied.IsSuccess)
        {
            await paymentsDbContext.SaveChangesAsync(cancellationToken);
            return applied.Map();
        }

        await paymentsDbContext.SaveChangesAsync(cancellationToken);
        return Result.Created(DonationResponse.From(donation, payment, created.Value.ApprovalUrl));
    }

    async Task<Result<Customer>> GetOrCreateCustomer(Guid userId, CancellationToken cancellationToken)
    {
        var customer = await paymentsDbContext.Customers
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (customer is not null)
            return Result.Success(customer);

        var created = Customer.Create(userId);
        if (!created.IsSuccess)
            return created;

        await paymentsDbContext.Customers.AddAsync(created.Value, cancellationToken);
        return created;
    }

    async Task<(Donation Donation, Payment Payment)?> FindByIdempotencyKey(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var payment = await paymentsDbContext.Payments
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (payment is null)
            return null;

        var donation = await paymentsDbContext.Donations
            .FirstOrDefaultAsync(x => x.Id == payment.ReferenceId, cancellationToken);
        if (donation is null)
            return null;

        return (donation, payment);
    }

    static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
