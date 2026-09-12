using Ardalis.Result.AspNetCore;
using Hub.Application.Abstractions;
using Hub.Application.Features.Payments.Commands.ConfirmDonation;
using Hub.Application.Features.Payments.Commands.CreateDonation;
using Hub.Application.Features.Payments.Contracts;
using Hub.Application.Features.Payments.Queries.GetDonationById;
using Hub.Application.Pipelines;
using Microsoft.AspNetCore.Mvc;

namespace Hub.Web.Endpoints;

static class DonationEndpoints
{
    public static void MapDonationEndpoints(this WebApplication app)
    {
        var donations = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/donations")
            .RequireAuthorization();

        donations.MapPost("/", Create)
            .HasApiVersion(1.0)
            .Produces<DonationResponse>(StatusCodes.Status201Created);

        donations.MapGet("/{donationId:guid}", GetById)
            .HasApiVersion(1.0)
            .Produces<DonationResponse>();

        donations.MapPost("/{donationId:guid}/confirm", Confirm)
            .HasApiVersion(1.0)
            .Produces<DonationResponse>();
    }

    static async Task<IResult> Create(
        [FromBody] CreateDonationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromServices] IUserContext userContext,
        [FromServices] IRequestHandler<CreateDonationCommand, DonationResponse> handler,
        CancellationToken ctk) =>
        (await handler.Handle(new(userContext.UserId, request, idempotencyKey), ctk))
        .ToMinimalApiResult();

    static async Task<IResult> GetById(
        [FromRoute] Guid donationId,
        [FromServices] IUserContext userContext,
        [FromServices] IRequestHandler<GetDonationByIdQuery, DonationResponse> handler,
        CancellationToken ctk) =>
        (await handler.Handle(new(userContext.UserId, donationId), ctk))
        .ToMinimalApiResult();

    static async Task<IResult> Confirm(
        [FromRoute] Guid donationId,
        [FromServices] IUserContext userContext,
        [FromServices] IRequestHandler<ConfirmDonationCommand, DonationResponse> handler,
        CancellationToken ctk) =>
        (await handler.Handle(new(userContext.UserId, donationId), ctk))
        .ToMinimalApiResult();
}
