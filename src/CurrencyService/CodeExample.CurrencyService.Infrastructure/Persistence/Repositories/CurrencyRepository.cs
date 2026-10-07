using System.Data.Common;
using Dapper;
using CodeExample.CurrencyService.Domain.Abstractions;
using CodeExample.CurrencyService.Domain.Entities;

namespace CodeExample.CurrencyService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Реализация <see cref="ICurrencyRepository"/>.
/// </summary>
public sealed class CurrencyRepository : ICurrencyRepository
{
    private const string SelectColumns = """
        SELECT id        AS "Id",
               name      AS "Name",
               rate      AS "Rate",
               is_active AS "IsActive"
        FROM currencies.currency
        """;

    private const string Upsert = """
        INSERT INTO currencies.currency (id, name, rate, is_active)
        SELECT id, name, rate, is_active FROM unnest(@Rows::currencies.currency_row[])
        ON CONFLICT (id) DO UPDATE
        SET name      = EXCLUDED.name,
            rate      = EXCLUDED.rate,
            is_active = EXCLUDED.is_active
        """;

    private const string DeactivateMissing = """
        UPDATE currencies.currency
        SET is_active = false
        WHERE id <> ALL(@Ids)
          AND is_active
        """;

    private readonly DbDataSource _dataSource;

    public CurrencyRepository(DbDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<Currency>> GetByIdsAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return Array.Empty<Currency>();
        }

        return await QueryAsync(
            $"{SelectColumns} WHERE id = ANY(@Ids)",
            new { Ids = ids.ToArray() },
            cancellationToken);
    }

    public Task<IReadOnlyList<Currency>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        QueryAsync($"{SelectColumns} WHERE is_active ORDER BY name", new { }, cancellationToken);

    public async Task<Currency?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        var row = await connection
            .QuerySingleOrDefaultAsync<CurrencyRow>(
                new CommandDefinition(
                    $"{SelectColumns} WHERE id = @Id",
                    new { Id = id },
                    cancellationToken: cancellationToken));

        return row is null ? null : CurrencyRow.ToEntity(row);
    }

    public async Task<int> SynchronizeAsync(
        IReadOnlyCollection<Currency> currencies,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currencies);

        var rows = ToRows(currencies);

        if (rows.Length == 0)
        {
            throw new ArgumentException(
                "Прогон синхронизации обязан нести хотя бы одну валюту.",
                nameof(currencies));
        }

        var ids = rows.Select(row => row.Id).ToArray();

        if (transaction is not null)
        {
            return await WriteAsync(transaction.Connection!, transaction, rows, ids, cancellationToken);
        }

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var ownTransaction = await connection.BeginTransactionAsync(cancellationToken);

        var written = await WriteAsync(connection, ownTransaction, rows, ids, cancellationToken);

        await ownTransaction.CommitAsync(cancellationToken);

        return written;
    }

    private static async Task<int> WriteAsync(
        DbConnection connection,
        DbTransaction transaction,
        CurrencyRow[] rows,
        string[] ids,
        CancellationToken cancellationToken)
    {
        var written = await connection
            .ExecuteAsync(new CommandDefinition(Upsert, new { Rows = rows }, transaction, cancellationToken: cancellationToken));

        written += await connection
            .ExecuteAsync(new CommandDefinition(DeactivateMissing, new { Ids = ids }, transaction, cancellationToken: cancellationToken));

        return written;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        return await connection
            .ExecuteScalarAsync<int>(
                new CommandDefinition(
                    "SELECT COUNT(*)::int FROM currencies.currency",
                    cancellationToken: cancellationToken));
    }

    private static CurrencyRow[] ToRows(IEnumerable<Currency> currencies) =>
        currencies.Select(CurrencyRow.From).ToArray();

    private async Task<IReadOnlyList<Currency>> QueryAsync(
        string sql,
        object parameters,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        var rows = await connection
            .QueryAsync<CurrencyRow>(
                new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return rows.Select(CurrencyRow.ToEntity).ToArray();
    }
}
