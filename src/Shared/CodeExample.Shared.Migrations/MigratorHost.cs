using System.Reflection;
using CodeExample.Shared.Database;
using CodeExample.Shared.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeExample.Shared.Migrations;

/// <summary>Хост мигратора: настройки, журнал и запуск скриптов.</summary>
public static class MigratorHost
{
    /// <param name="args">Аргументы командной строки.</param>
    /// <param name="connectionStringName">Имя строки подключения в разделе ConnectionStrings.</param>
    /// <param name="serviceName">Имя для сообщений журнала.</param>
    /// <param name="schema">Схема, которой владеет сервис.</param>
    /// <param name="journalTable">Таблица журнала миграций внутри схемы.</param>
    /// <param name="scriptsAssembly">Сборка, в которую встроены скрипты.</param>
    /// <returns>0 при успехе, 1 при отказе.</returns>
    public static async Task<int> RunAsync(
        string[] args,
        string connectionStringName,
        string serviceName,
        string schema,
        string journalTable,
        Assembly scriptsAssembly)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        var builder = Host
            .CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                Args = args,
                ContentRootPath = AppContext.BaseDirectory
            })
            .ConfigureSettings()
            .ConfigureLogging();

        string connectionString;

        try
        {
            // Отсутствующая строка подключения это не сбой миграции, а незапущенный шаг
            // развёртывания: сообщение уходит в stderr, а хост возвращает код 1.
            connectionString = builder.Configuration.RequireConnectionString(connectionStringName);
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);

            return 1;
        }

        using var host = builder.Build();

        var logger = host.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Migrator");

        return await MigratorRunner.RunAsync(
            connectionString,
            serviceName,
            schema,
            journalTable,
            scriptsAssembly,
            logger);
    }
}
