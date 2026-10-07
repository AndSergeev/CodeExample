using System.Reflection;
using CodeExample.Shared.Database;
using DbUp;
using Microsoft.Extensions.Logging;

namespace CodeExample.Shared.Migrations;

/// <summary>Применяет встроенные SQL-скрипты сборки, которые ещё не применялись.</summary>
public static class MigratorRunner
{
    /// <param name="connectionString">База, которой принадлежит схема.</param>
    /// <param name="serviceName">Имя для сообщений журнала.</param>
    /// <param name="schema">Схема, в которой создаётся таблица журнала миграций.</param>
    /// <param name="journalTable">Таблица журнала внутри схемы.</param>
    /// <param name="scriptsAssembly">Сборка, в которую встроены скрипты.</param>
    /// <param name="logger">Журнал мигрирующего хоста.</param>
    /// <returns>0 при успехе, 1 при отказе.</returns>
    public static async Task<int> RunAsync(
        string connectionString,
        string serviceName,
        string schema,
        string journalTable,
        Assembly scriptsAssembly,
        ILogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(journalTable);
        ArgumentNullException.ThrowIfNull(scriptsAssembly);
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            logger.LogInformation("Применение миграций {ServiceName}", serviceName);

            // Схема должна существовать до того, как DbUp тронет свою таблицу журнала.
            await SqlScriptMigrator.EnsureSchemaAsync(connectionString, schema);

            var upgrader = DeployChanges
                .To
                .PostgresqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(scriptsAssembly)
                // DbUp выполняет каждый скрипт в своей транзакции и записывает его,
                // поэтому повторный запуск ничего не применяет заново.
                .JournalToPostgresqlTable(schema, journalTable)
                .LogToConsole()
                .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                logger.LogCritical(
                    result.Error,
                    "Применение миграций {ServiceName} завершилось отказом",
                    serviceName);

                return 1;
            }

            logger.LogInformation(
                "Схема {ServiceName} актуальна: применено скриптов {MigrationCount}",
                serviceName,
                result.Scripts.Count());

            foreach (var script in result.Scripts)
            {
                logger.LogDebug("Применена миграция {Migration}", script.Name);
            }

            return 0;
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                exception,
                "Применение миграций {ServiceName} завершилось сбоем",
                serviceName);

            return 1;
        }
        finally
        {
            Serilog.Log.CloseAndFlush();
        }
    }
}
