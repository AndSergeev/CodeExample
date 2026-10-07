using System.Data.Common;
using CodeExample.FinanceService.Domain.Entities;

namespace CodeExample.FinanceService.Domain.Abstractions;

/// <summary>Контракт хранения валют, которые отслеживает пользователь.</summary>
public interface IUserFavoriteCurrencyRepository
{
    /// <summary>Возвращает валюты пользователя.</summary>
    Task<IReadOnlyList<UserFavoriteCurrency>> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет валюту в избранное.
    /// </summary>
    /// <remarks>
    /// Вставка идемпотентна: <c>INSERT ... ON CONFLICT DO NOTHING</c> ничего не пишет, если
    /// пару уже отслеживают, и вызывающий узнаёт об этом по числу строк.
    /// </remarks>
    /// <returns>1, если отслеживание начал этот вызов, 0, если валюту уже отслеживали.</returns>
    Task<int> AddAsync(
        UserFavoriteCurrency favorite,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>Удаляет валюту из избранного.</summary>
    /// <returns>Число удалённых строк: 0, если ничего не отслеживалось.</returns>
    Task<int> RemoveAsync(Guid userId, string currencyId, CancellationToken cancellationToken = default);
}
