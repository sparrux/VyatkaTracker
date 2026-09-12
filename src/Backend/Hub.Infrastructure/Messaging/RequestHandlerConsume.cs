using Ardalis.Result;
using Hub.Application.Features.Common.Contracts;
using Hub.Application.Pipelines;

namespace Hub.Infrastructure.Messaging;

static class RequestHandlerConsume
{
    public static async Task Consume<TRequest, TResponse>(
        IRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
        var result = await handler.Handle(request, cancellationToken);
        if (result.IsSuccess)
            return;

        throw new InvalidOperationException(Format(result));
    }

    static string Format<TResponse>(Result<TResponse> result)
    {
        var errors = result.Errors
            .Concat(result.ValidationErrors.Select(error => error.ErrorMessage))
            .Where(error => !string.IsNullOrWhiteSpace(error));

        return string.Join("; ", errors);
    }
}
