using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using CodeExample.CurrencyService.Contracts;
using CodeExample.FinanceService.Contracts;
using CodeExample.FinanceService.Domain.Abstractions;
using CodeExample.Shared.Abstractions;

namespace CodeExample.FinanceService.Application.Favorites.GetFavoriteCurrencies;

/// <summary>
/// Перечисляет валюты, которые отслеживает аутентифицированный пользователь, с текущими курсами.
/// </summary>
/// <param name="UserId">Id пользователя, берётся из проверенного access-токена.</param>
public sealed record GetFavoriteCurrenciesQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<FavoriteRateResponse>>>;

internal sealed class GetFavoriteCurrenciesQueryValidator : AbstractValidator<GetFavoriteCurrenciesQuery>
{
    public GetFavoriteCurrenciesQueryValidator() =>
        RuleFor(x => x.UserId).NotEmpty().WithMessage("A user id is required.");
}

/// <inheritdoc />
internal sealed class GetFavoriteCurrenciesQueryHandler
    : IRequestHandler<GetFavoriteCurrenciesQuery, Result<IReadOnlyList<FavoriteRateResponse>>>
{
    private readonly IUserFavoriteCurrencyRepository _favorites;
    private readonly ICurrencyClient _currencyClient;
    private readonly ILogger<GetFavoriteCurrenciesQueryHandler> _logger;

    public GetFavoriteCurrenciesQueryHandler(
        IUserFavoriteCurrencyRepository favorites,
        ICurrencyClient currencyClient,
        ILogger<GetFavoriteCurrenciesQueryHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(favorites);
        ArgumentNullException.ThrowIfNull(currencyClient);
        ArgumentNullException.ThrowIfNull(logger);

        _favorites = favorites;
        _currencyClient = currencyClient;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<FavoriteRateResponse>>> Handle(
        GetFavoriteCurrenciesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var favorites = await _favorites.GetAsync(request.UserId, cancellationToken);

        if (favorites.Count == 0)
        {
            _logger.LogInformation("Пользователь {UserId} не отслеживает ни одной валюты", request.UserId);
            return Result.Success<IReadOnlyList<FavoriteRateResponse>>(Array.Empty<FavoriteRateResponse>());
        }

        var currencyIds = favorites.Select(favorite => favorite.CurrencyId).ToArray();

        IReadOnlyList<CurrencyResponse> currencies;

        try
        {
            currencies = await _currencyClient.GetByIdsAsync(currencyIds, cancellationToken);
        }
        catch (CurrencyReferenceException exception)
        {
            _logger.LogError(
                exception,
                "Не удалось разрешить курсы в количестве {CurrencyCount} для пользователя {UserId}",
                currencyIds.Length,
                request.UserId);

            return Result.Failure<IReadOnlyList<FavoriteRateResponse>>(Error.Unavailable(
                "currency_reference.unavailable",
                "The currency service could not be reached."));
        }

        var currenciesById = currencies.ToDictionary(currency => currency.Id);

        var response = new List<FavoriteRateResponse>(favorites.Count);

        foreach (var favorite in favorites)
        {
            if (currenciesById.TryGetValue(favorite.CurrencyId, out var currency))
            {
                response.Add(favorite.ToRateResponse(currency));
            }
            else
            {
                _logger.LogWarning(
                    "Валюту {CurrencyId}, отслеживаемую пользователем {UserId}, не удалось разрешить в сервисе курсов",
                    favorite.CurrencyId,
                    request.UserId);
            }
        }

        return Result.Success<IReadOnlyList<FavoriteRateResponse>>(response);
    }
}
