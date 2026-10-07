namespace CodeExample.CurrencyService.Contracts;

/// <summary>
/// Валюта в том виде, в каком её публикует сервис курсов.
/// </summary>
/// <param name="Id">Идентификатор ЦБ РФ, например R01235.</param>
/// <param name="Name">Название, как его публикует ЦБ РФ.</param>
/// <param name="Rate">
/// Рублей ровно за одну единицу валюты: сервис делит на номинал, который назвал банк,
/// поэтому умножать сумму на этот курс можно напрямую.
/// </param>
public sealed record CurrencyResponse(
    string Id,
    string Name,
    decimal Rate);

/// <summary>
/// Тело запроса поиска.
/// </summary>
/// <param name="Ids">Идентификаторы валют.</param>
public sealed record CurrenciesByIdsRequest(IReadOnlyCollection<string> Ids);
