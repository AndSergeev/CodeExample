using CodeExample.CurrencyService.Contracts;
using CodeExample.FinanceService.Contracts;
using CodeExample.FinanceService.Domain.Entities;

namespace CodeExample.FinanceService.Application.Favorites;

/// <summary>
/// Маппит домен и контракты сервиса курсов на контракты этого сервиса;
/// </summary>
public static class FavoriteMappings
{
    public static FavoriteCurrencyResponse ToResponse(this UserFavoriteCurrency favorite)
    {
        ArgumentNullException.ThrowIfNull(favorite);

        return new FavoriteCurrencyResponse(favorite.CurrencyId);
    }

    /// <summary>Соединяет валюту с курсом из сервиса курсов.</summary>
    public static FavoriteRateResponse ToRateResponse(
        this UserFavoriteCurrency favorite,
        CurrencyResponse currency)
    {
        ArgumentNullException.ThrowIfNull(favorite);
        ArgumentNullException.ThrowIfNull(currency);

        return new FavoriteRateResponse(
            currency.Id,
            currency.Name,
            currency.Rate);
    }
}
