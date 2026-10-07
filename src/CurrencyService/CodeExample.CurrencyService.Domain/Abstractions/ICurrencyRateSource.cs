namespace CodeExample.CurrencyService.Domain.Abstractions;

/// <summary>
/// Курс в том виде, в каком его опубликовал ЦБ РФ.
/// </summary>
/// <param name="Id">Идентификатор ЦБ РФ, например R01235.</param>
/// <param name="Name">Название валюты.</param>
/// <param name="Nominal">На сколько единиц рассчитана опубликованная цена.</param>
/// <param name="Value">Цена этого числа единиц в рублях.</param>
public sealed record CurrencyRate(string Id, string Name, int Nominal, decimal Value);

/// <summary>
/// Источник курсов.
/// </summary>
public interface ICurrencyRateSource
{
    /// <summary>Забирает курсы на день.</summary>
    Task<Shared.Abstractions.Result<IReadOnlyList<CurrencyRate>>> FetchDailyRatesAsync(
        CancellationToken cancellationToken = default);
}
