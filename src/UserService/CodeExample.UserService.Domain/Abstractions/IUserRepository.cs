using System.Data.Common;
using CodeExample.UserService.Domain.Entities;

namespace CodeExample.UserService.Domain.Abstractions;

/// <summary>Контракт хранения User.</summary>
public interface IUserRepository
{
    /// <summary>Ищет пользователя по логину - null, если такого нет.</summary>
    Task<User?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ищет пользователя по идентификатору - null, если такого нет.
    /// </summary>
    /// <remarks>
    /// Вызывающий передаёт транзакцию, когда строка уже заперта его же незакоммиченной
    /// записью: читая через другое соединение из пула, он не увидел бы собственных изменений.
    /// </remarks>
    Task<User?> FindByIdAsync(
        Guid id,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>Проверяет, занят ли логин.</summary>
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Записывает нового пользователя, если логин ещё свободен.
    /// </summary>
    /// <remarks>
    /// Проверка занятости и вставка разведены, поэтому между ними помещается второй запрос
    /// с тем же логином. Здесь это не ошибка: <c>INSERT ... ON CONFLICT DO NOTHING</c> ничего
    /// не пишет, а вызывающий узнаёт о проигранной гонке по числу строк.
    /// </remarks>
    /// <returns>Число записанных строк: <c>1</c> или <c>0</c>, если логин уже занят.</returns>
    Task<int> AddAsync(User user, DbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Гасит активную сессию по хешу токена и сообщает, чем закончилась попытка.
    /// </summary>
    /// <remarks>
    /// Гашение и разбор случая - одна инструкция, а не чтение с последующей записью: между
    /// ними помещался бы второй запрос с тем же токеном, и оба владельца получали бы по сессии.
    /// Вызывающий передаёт транзакцию, потому что гашение старой сессии и запись новой
    /// обязаны состояться вместе: сбой между ними уничтожил бы сессию без замены.
    /// </remarks>
    /// <returns>Исход попытки, или <c>null</c>, если сессии с таким хешем нет вовсе.</returns>
    Task<RefreshTokenConsumptionResult?> ConsumeAsync(
        string tokenHash,
        DateTime now,
        DateTime revokedAtUtc,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>Записывает новую сессию обновления.</summary>
    Task<int> AddRefreshTokenAsync(
        RefreshToken refreshToken,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>Отзывает одну сессию пользователя.</summary>
    /// <returns>Сколько сессий отозвано.</returns>
    Task<int> RevokeRefreshTokenAsync(
        Guid userId,
        string tokenHash,
        DateTime now,
        CancellationToken cancellationToken = default);

    /// <summary>Отзывает все активные сессии пользователя.</summary>
    /// <returns>Сколько сессий отозвано.</returns>
    Task<int> RevokeAllRefreshTokensAsync(
        Guid userId,
        DateTime now,
        CancellationToken cancellationToken = default);
}
