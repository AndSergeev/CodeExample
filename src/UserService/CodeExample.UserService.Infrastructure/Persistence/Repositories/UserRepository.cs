using System.Data.Common;
using Dapper;
using CodeExample.UserService.Domain.Abstractions;
using CodeExample.UserService.Domain.Entities;

namespace CodeExample.UserService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Реализация <see cref="IUserRepository"/>.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private const string SelectUser = """
        SELECT id              AS "Id",
               name            AS "Name",
               password        AS "Password",
               created_at_utc  AS "CreatedAtUtc"
        FROM users.user
        """;

    private readonly DbDataSource _dataSource;

    public UserRepository(DbDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        _dataSource = dataSource;
    }

    public Task<User?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return QueryUserAsync($"{SelectUser} WHERE name = @Name", new { Name = name }, null, cancellationToken);
    }

    public Task<User?> FindByIdAsync(
        Guid id,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default) =>
        QueryUserAsync($"{SelectUser} WHERE id = @Id", new { Id = id }, transaction, cancellationToken);

    public async Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        await using var connection = await OpenAsync(cancellationToken);

        return await connection
            .ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    "SELECT EXISTS (SELECT 1 FROM users.user WHERE name = @Name)",
                    new { Name = name },
                    cancellationToken: cancellationToken));
    }

    public Task<int> AddAsync(
        User user,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        return ExecuteAsync(
            """
            INSERT INTO users.user (id, name, password, created_at_utc)
            VALUES (@Id, @Name, @Password, @CreatedAtUtc)
            ON CONFLICT (name) DO NOTHING
            """,
            new { user.Id, user.Name, user.Password, user.CreatedAtUtc },
            transaction,
            cancellationToken);
    }

    public async Task<RefreshTokenConsumptionResult?> ConsumeAsync(
        string tokenHash,
        DateTime now,
        DateTime revokedAtUtc,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        // Разбор случая идёт в одном запросе с гашением. Блокировка строки не даёт двум
        // запросам с одной копией токена прочитать её одновременно.
        const string sql = """
            WITH candidate AS (
                SELECT id, user_id, revoked_at_utc, expires_at_utc, replaced_at_utc
                FROM users.refresh_token
                WHERE token_hash = @TokenHash
                FOR UPDATE
            ),
            consumed AS (
                UPDATE users.refresh_token t
                SET revoked_at_utc = @RevokedAtUtc,
                    replaced_at_utc = @RevokedAtUtc
                FROM candidate c
                WHERE t.id = c.id
                  AND c.revoked_at_utc IS NULL
                  AND c.expires_at_utc > @Now
                RETURNING t.user_id
            )
            SELECT CASE
                       WHEN EXISTS (SELECT 1 FROM consumed) THEN 0
                       -- Срок проверяется раньше замены: просроченный токен мёртв независимо
                       -- от того, обменяли его когда-то, и живой пары у вызывающего уже нет.
                       WHEN (SELECT expires_at_utc FROM candidate) <= @Now THEN 2
                       WHEN (SELECT replaced_at_utc FROM candidate) IS NOT NULL THEN 1
                       ELSE 2
                   END AS "Outcome",
                   (SELECT user_id FROM candidate) AS "UserId",
                   EXISTS (SELECT 1 FROM candidate) AS "Found"
            """;

        var parameters = new
        {
            TokenHash = tokenHash,
            Now = now,
            RevokedAtUtc = revokedAtUtc
        };

        var row = transaction is not null
            ? await transaction.Connection!
                .QuerySingleAsync<ConsumptionRow>(
                    new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken))
            : await QueryConsumptionAsync(sql, parameters, cancellationToken);

        return row.Found
            ? new RefreshTokenConsumptionResult((RefreshTokenConsumption)row.Outcome, row.UserId)
            : null;
    }

    private async Task<ConsumptionRow> QueryConsumptionAsync(
        string sql,
        object parameters,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);

        return await connection
            .QuerySingleAsync<ConsumptionRow>(
                new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    private sealed class ConsumptionRow
    {
        public int Outcome { get; init; }

        public Guid UserId { get; init; }

        public bool Found { get; init; }
    }

    public Task<int> AddRefreshTokenAsync(
        RefreshToken refreshToken,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);

        return ExecuteAsync(
            """
            INSERT INTO users.refresh_token
                (id, user_id, token_hash, created_at_utc, expires_at_utc, revoked_at_utc)
            VALUES
                (@Id, @UserId, @TokenHash, @CreatedAtUtc, @ExpiresAtUtc, @RevokedAtUtc)
            """,
            new
            {
                refreshToken.Id,
                refreshToken.UserId,
                refreshToken.TokenHash,
                refreshToken.CreatedAtUtc,
                refreshToken.ExpiresAtUtc,
                refreshToken.RevokedAtUtc
            },
            transaction,
            cancellationToken);
    }

    public async Task<int> RevokeRefreshTokenAsync(        Guid userId,
        string tokenHash,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        const string sql = """
            UPDATE users.refresh_token
            SET revoked_at_utc = @Now
            WHERE user_id = @UserId
              AND token_hash = @TokenHash
              AND revoked_at_utc IS NULL
            """;

        await using var connection = await OpenAsync(cancellationToken);

        return await connection
            .ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { UserId = userId, TokenHash = tokenHash, Now = now },
                    cancellationToken: cancellationToken));
    }

    public async Task<int> RevokeAllRefreshTokensAsync(
        Guid userId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE users.refresh_token
            SET revoked_at_utc = @Now
            WHERE user_id = @UserId
              AND revoked_at_utc IS NULL
            """;

        await using var connection = await OpenAsync(cancellationToken);

        return await connection
            .ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { UserId = userId, Now = now },
                    cancellationToken: cancellationToken));
    }

    private ValueTask<DbConnection> OpenAsync(CancellationToken cancellationToken) =>
        _dataSource.OpenConnectionAsync(cancellationToken);

    private async Task<int> ExecuteAsync(
        string sql,
        object parameters,
        DbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            return await transaction.Connection!
                .ExecuteAsync(new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken));
        }

        await using var connection = await OpenAsync(cancellationToken);

        return await connection
            .ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    private async Task<User?> QueryUserAsync(
        string sql,
        object parameters,
        DbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            var locked = await transaction.Connection!
                .QuerySingleOrDefaultAsync<UserRow>(
                    new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken));

            return locked is null ? null : UserRow.ToEntity(locked);
        }

        await using var connection = await OpenAsync(cancellationToken);

        var row = await connection
            .QuerySingleOrDefaultAsync<UserRow>(
                new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return row is null ? null : UserRow.ToEntity(row);
    }
}
