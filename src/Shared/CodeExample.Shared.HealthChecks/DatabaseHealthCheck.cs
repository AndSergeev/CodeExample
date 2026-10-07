using System.Data.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CodeExample.Shared.HealthChecks;

/// <summary>
/// Сообщает, отвечает ли база сервиса.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    public const string Name = "database";

    private readonly DbDataSource _dataSource;

    public DatabaseHealthCheck(DbDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        _dataSource = dataSource;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _dataSource
                .OpenConnectionAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";

            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("База данных ответила на пробный запрос.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(
                "База данных не ответила на пробный запрос.",
                exception);
        }
    }
}
