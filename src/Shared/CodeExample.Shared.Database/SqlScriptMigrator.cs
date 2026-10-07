using Npgsql;

namespace CodeExample.Shared.Database;

/// <summary>
/// Создаёт схему сервиса до того, как DbUp возьмётся за свои таблицы.
/// </summary>
public static class SqlScriptMigrator
{
    public static async Task EnsureSchemaAsync(
        string connectionString,
        string schema,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource
            .OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{schema}\"";

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
