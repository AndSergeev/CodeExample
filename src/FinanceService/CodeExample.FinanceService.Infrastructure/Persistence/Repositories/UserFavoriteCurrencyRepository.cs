using System.Data.Common;
using Dapper;
using CodeExample.FinanceService.Domain.Abstractions;
using CodeExample.FinanceService.Domain.Entities;
using CodeExample.FinanceService.Infrastructure.Persistence;

namespace CodeExample.FinanceService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Реализация <see cref="IUserFavoriteCurrencyRepository"/>.
/// </summary>
public sealed class UserFavoriteCurrencyRepository : IUserFavoriteCurrencyRepository
{
    private const string SelectColumns = """
        SELECT user_id     AS "UserId",
               currency_id AS "CurrencyId"
        FROM finance.user_favorite_currency
        """;

    private const string Insert = """
        INSERT INTO finance.user_favorite_currency (user_id, currency_id)
        VALUES (@UserId, @CurrencyId)
        ON CONFLICT (user_id, currency_id) DO NOTHING
        """;

    private readonly DbDataSource _dataSource;

    public UserFavoriteCurrencyRepository(DbDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<UserFavoriteCurrency>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        var rows = await connection
            .QueryAsync<UserFavoriteCurrencyRow>(
                new CommandDefinition(
                    $"{SelectColumns} WHERE user_id = @UserId ORDER BY currency_id",
                    new { UserId = userId },
                    cancellationToken: cancellationToken));

        return rows.Select(UserFavoriteCurrencyRow.ToEntity).ToArray();
    }

    public async Task<int> AddAsync(
        UserFavoriteCurrency favorite,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(favorite);

        if (transaction is not null)
        {
            return await transaction.Connection!
                .ExecuteAsync(
                    new CommandDefinition(
                        Insert,
                        new { favorite.UserId, favorite.CurrencyId },
                        transaction,
                        cancellationToken: cancellationToken));
        }

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        return await connection
            .ExecuteAsync(
                new CommandDefinition(
                    Insert,
                    new { favorite.UserId, favorite.CurrencyId },
                    cancellationToken: cancellationToken));
    }

    public async Task<int> RemoveAsync(
        Guid userId,
        string currencyId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM finance.user_favorite_currency
            WHERE user_id = @UserId AND currency_id = @CurrencyId
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        return await connection
            .ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { UserId = userId, CurrencyId = currencyId },
                    cancellationToken: cancellationToken));
    }
}
