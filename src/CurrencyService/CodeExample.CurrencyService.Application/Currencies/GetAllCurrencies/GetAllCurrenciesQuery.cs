using MediatR;
using CodeExample.CurrencyService.Domain.Abstractions;
using CodeExample.Shared.Abstractions;
using CodeExample.CurrencyService.Contracts;

namespace CodeExample.CurrencyService.Application.Currencies.GetAllCurrencies;

/// <summary>Возвращает валюты, которые ЦБ публикует сейчас.</summary>
public sealed record GetAllCurrenciesQuery : IRequest<Result<IReadOnlyList<CurrencyResponse>>>;

/// <inheritdoc />
internal sealed class GetAllCurrenciesQueryHandler
    : IRequestHandler<GetAllCurrenciesQuery, Result<IReadOnlyList<CurrencyResponse>>>
{
    private readonly ICurrencyRepository _repository;

    public GetAllCurrenciesQueryHandler(ICurrencyRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<CurrencyResponse>>> Handle(
        GetAllCurrenciesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currencies = await _repository.GetActiveAsync(cancellationToken);

        IReadOnlyList<CurrencyResponse> response = currencies
            .Select(currency => currency.ToResponse())
            .ToList();

        return Result.Success(response);
    }
}
