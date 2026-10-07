using System.Data.Common;
using CodeExample.CurrencyService.Domain.Entities;

namespace CodeExample.CurrencyService.Domain.Abstractions;

/// <summary>Контракт хранения агрегата валюты.</summary>
public interface ICurrencyRepository
{
    /// <summary>Возвращает валюты, которые банк публикует сейчас, по порядку названий.</summary>
    Task<IReadOnlyList<Currency>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Возвращает несколько валют по идентификаторам, включая снятые с публикации.</summary>
    /// <remarks>
    /// Неактивные строки возвращаются намеренно: по ним видно, что валюта когда-то
    /// была в курсах ЦБ, а теперь из них пропала.
    /// </remarks>
    Task<IReadOnlyList<Currency>> GetByIdsAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken cancellationToken = default);

    /// <summary>Находит валюту по идентификатору или возвращает null.</summary>
    Task<Currency?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Записывает валюты одного прогона синхронизации: вставляет новые, обновляет название
    /// и курс у существующих, а пропавшие из ответа ЦБ помечает неактивными.
    /// </summary>
    /// <remarks>
    /// Вернувшаяся в ответе валюта снова становится активной, поэтому хранилище ничего
    /// не удаляет: неактивная строка это «банк её больше не публикует», а не «её не было».
    /// </remarks>
    /// <param name="currencies">Валюты.</param>
    /// <param name="transaction">Транзакция.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Сумма двух чисел: строк, затронутых вставкой или обновлением, и строк, погашенных деактивацией.</returns>
    Task<int> SynchronizeAsync(
        IReadOnlyCollection<Currency> currencies,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>Число сохранённых валют, активных и неактивных.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
