namespace Hub.Application.Features.Payments.Contracts;

public sealed record MoneyResponse(
    decimal Amount,
    string Currency
);
