using Microsoft.Extensions.Configuration;

namespace CodeExample.Shared.Database;

/// <summary>Чтение строк подключения из конфигурации.</summary>
public static class ConnectionStringExtensions
{
    /// <summary>Читает строку подключения или падает, если она не задана.</summary>
    public static string RequireConnectionString(
        this IConfiguration configuration,
        string name)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var connectionString = configuration.GetConnectionString(name);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Строка подключения '{name}' не настроена. " +
                $"Задайте ConnectionStrings:{name} в appsettings или через переменные среды.");
        }

        return connectionString;
    }
}
