namespace CodeExample.FinanceService.Contracts;

/// <summary>
/// Валюта, которую только что добавили в избранное.
/// </summary>
public sealed record FavoriteCurrencyResponse(string CurrencyId);

/// <summary>Отслеживаемая валюта вместе с текущим курсом.</summary>
/// <param name="Rate">Рублей ровно за одну единицу валюты.</param>
public sealed record FavoriteRateResponse(
    string CurrencyId,
    string Name,
    decimal Rate);

/// <summary>Тело запроса на добавление валюты в избранное.</summary>
public sealed record AddFavoriteCurrencyRequest(string CurrencyId);

/// <summary>
/// Пути finance-эндпоинтов.
/// </summary>
public static class FinanceRoutes
{
    public const string FavoritesBase = "api/finance/favorites";

    public const string ByCurrencyId = "{currencyId}";

    public const string FavoritesPath = $"/{FavoritesBase}";
}
