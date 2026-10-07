using System.Data.Common;

namespace CodeExample.UserService.Domain.Abstractions;

/// <summary>
/// Одно соединение и одна транзакция над ним, для сценария с несколькими репозиториями.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task<DbTransaction> BeginAsync(CancellationToken cancellationToken = default);

    /// <summary>Коммитит транзакцию, начатую <see cref="BeginAsync"/>.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);
}
