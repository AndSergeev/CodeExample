using CodeExample.CurrencyService.Domain.Entities;
using CodeExample.CurrencyService.Contracts;

namespace CodeExample.CurrencyService.Application.Currencies;

/// <summary>
/// Маппит агрегат валюты на контракт, который публикует сервис.
/// </summary>
public static class CurrencyMappings
{
    public static CurrencyResponse ToResponse(this Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        return new CurrencyResponse(
            currency.Id,
            currency.Name,
            currency.Rate);
    }
}
