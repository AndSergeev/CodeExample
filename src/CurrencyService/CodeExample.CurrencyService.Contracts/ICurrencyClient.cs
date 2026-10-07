namespace CodeExample.CurrencyService.Contracts;

/// <summary>
/// Сервис курсов в том виде, в каком его используют потребители.
/// </summary>
public interface ICurrencyClient
{
    /// <summary>
    /// Возвращает валюты по их идентификаторам.
    /// </summary>
    /// <param name="currencyIds">Идентификаторы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>
    /// Найденные валюты в произвольном порядке, никогда не null.
    /// </returns>
    /// <exception cref="CurrencyReferenceException">
    /// Сервис недоступен или ответил тем, что нельзя прочитать.
    /// </exception>
    Task<IReadOnlyList<CurrencyResponse>> GetByIdsAsync(
        IReadOnlyCollection<string> currencyIds,
        CancellationToken cancellationToken = default);
}
