using Ardalis.Result.AspNetCore;
using Hub.Application.Features.Payments.Commands.CancelCashDonation;
using Hub.Application.Features.Payments.Commands.ConfirmCashDonation;
using Hub.Application.Features.Payments.Commands.RecordCashDonation;
using Hub.Application.Features.Payments.Contracts;
using Hub.Application.Features.Payments.Queries.GetAdminDonationById;
using Hub.Application.Pipelines;
using Hub.Web.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Hub.Web.Endpoints;

static class AdminDonationEndpoints
{
    public static void MapAdminDonationEndpoints(this WebApplication app)
    {
        var donations = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/admin/donations")
            .RequireAuthorization(AuthorizationPolicies.Admin);

        donations.MapPost("/cash", RecordCash)
            .HasApiVersion(1.0)
            .Produces<DonationResponse>(StatusCodes.Status201Created);

        donations.MapGet("/{donationId:guid}", GetById)
            .HasApiVersion(1.0)
            .Produces<DonationResponse>();

        donations.MapPost("/{donationId:guid}/confirm", ConfirmCash)
            .HasApiVersion(1.0)
            .Produces<DonationResponse>();

        donations.MapPost("/{donationId:guid}/cancel", CancelCash)
            .HasApiVersion(1.0)
            .Produces<DonationResponse>();
    }

    static async Task<IResult> RecordCash(
        [FromBody] RecordCashDonationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromServices] IRequestHandler<RecordCashDonationCommand, DonationResponse> handler,
        CancellationToken ctk) =>
        (await handler.Handle(new(request, idempotencyKey), ctk))
        .ToMinimalApiResult();

    static async Task<IResult> GetById(
        [FromRoute] Guid donationId,
        [FromServices] IRequestHandler<GetAdminDonationByIdQuery, DonationResponse> handler,
        CancellationToken ctk) =>
        (await handler.Handle(new(donationId), ctk))
        .ToMinimalApiResult();

    static async Task<IResult> ConfirmCash(
        [FromRoute] Guid donationId,
        [FromServices] IRequestHandler<ConfirmCashDonationCommand, DonationResponse> handler,
        CancellationToken ctk) =>
        (await handler.Handle(new(donationId), ctk))
        .ToMinimalApiResult();

    static async Task<IResult> CancelCash(
        [FromRoute] Guid donationId,
        [FromServices] IRequestHandler<CancelCashDonationCommand, DonationResponse> handler,
        CancellationToken ctk) =>
        (await handler.Handle(new(donationId), ctk))
        .ToMinimalApiResult();
}
