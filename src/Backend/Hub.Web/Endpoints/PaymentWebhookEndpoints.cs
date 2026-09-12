using Ardalis.Result.AspNetCore;
using Hub.Application.Features.Common.Contracts;
using Hub.Application.Features.Payments.Commands.ReceivePaymentWebhook;
using Hub.Application.Pipelines;
using Microsoft.AspNetCore.Mvc;

namespace Hub.Web.Endpoints;

static class PaymentWebhookEndpoints
{
    public static void MapPaymentWebhookEndpoints(this WebApplication app)
    {
        var webhooks = app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}/payments/webhooks")
            .AllowAnonymous();

        webhooks.MapPost("/{provider}", Receive)
            .HasApiVersion(1.0)
            .Produces<IdResponse>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status400BadRequest);
    }

    static async Task<IResult> Receive(
        [FromRoute] string provider,
        HttpRequest request,
        [FromServices] IRequestHandler<ReceivePaymentWebhookCommand, IdResponse> handler,
        CancellationToken ctk)
    {
        using var reader = new StreamReader(request.Body);
        var payload = await reader.ReadToEndAsync(ctk);

        var headers = request.Headers.ToDictionary(
            header => header.Key,
            header => string.Join(',', header.Value.ToArray()),
            StringComparer.OrdinalIgnoreCase);

        var result = await handler.Handle(new(provider, payload, headers), ctk);
        if (!result.IsSuccess)
            return result.ToMinimalApiResult();

        return Results.Accepted(value: result.Value);
    }
}
