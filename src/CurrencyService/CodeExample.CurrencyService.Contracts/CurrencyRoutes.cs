namespace CodeExample.CurrencyService.Contracts;

/// <summary>
/// Пути эндпоинтов валют.
/// </summary>
public static class CurrencyRoutes
{
    /// <summary>Базовый путь эндпоинтов валют.</summary>
    public const string Base = "internal/currencies";

    /// <summary>Пакетный запрос.</summary>
    public const string ByIds = "by-ids";

    /// <summary>Запрос одной валюты.</summary>
    public const string ById = "{id}";
}
