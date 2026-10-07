using System.Data.Common;
using CodeExample.UserService.Domain.Abstractions;

namespace CodeExample.UserService.Infrastructure.Persistence;

/// <inheritdoc />
public sealed class SqlUnitOfWork : IUnitOfWork
{
    private readonly DbDataSource _dataSource;

    private DbConnection? _connection;
    private DbTransaction? _transaction;
    private bool _committed;

    public SqlUnitOfWork(DbDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        _dataSource = dataSource;
    }

    public async Task<DbTransaction> BeginAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException(
                "Эта единица работы уже начала транзакцию. Закоммитьте её или возьмите другую единицу работы.");
        }

        _connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        _transaction = await _connection.BeginTransactionAsync(cancellationToken);

        return _transaction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException(
                "Коммитить нечего: транзакция не начата. Сначала вызовите BeginAsync.");
        }

        await _transaction.CommitAsync(cancellationToken);
        _committed = true;
    }

    public async ValueTask DisposeAsync()
    {
        var connection = _connection;
        var transaction = _transaction;

        _connection = null;
        _transaction = null;

        if (transaction is not null)
        {
            if (!_committed)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception exception) when (IsAbandonedTransaction(exception))
                {
                }
            }

            await transaction.DisposeAsync();
        }

        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
    }

    public void Dispose()
    {
        var connection = _connection;
        var transaction = _transaction;

        _connection = null;
        _transaction = null;

        if (transaction is not null)
        {
            if (!_committed)
            {
                try
                {
                    transaction.Rollback();
                }
                catch (Exception exception) when (IsAbandonedTransaction(exception))
                {
                }
            }

            transaction.Dispose();
        }

        connection?.Dispose();
    }

    /// <summary>
    /// Отличает отказ отката, который нечего исправлять, от ошибки, которую нужно показать.
    /// </summary>
    /// <remarks>
    /// Неудавшаяся инструкция или оборванное соединение оставляют транзакцию уже освобождённой,
    /// и откат в таком состоянии - часть уборки, а не ошибка вызывающего. Пропустив её наружу,
    /// мы подменили бы объявленный отказ (409 при занятом логине, 401 при мёртвом токене)
    /// на 500. Отмена сюда не входит намеренно: по ней нельзя заключить, что транзакция
    /// мертва, а исключение из Dispose потеряло бы исходную причину запроса.
    /// </remarks>
    private static bool IsAbandonedTransaction(Exception exception) =>
        exception is DbException or InvalidOperationException or ObjectDisposedException;
}
