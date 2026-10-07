using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CodeExample.Shared.Database;

/// <summary>
/// Подготовка пула соединений, из которого берёт соединения сервис.
/// </summary>
public static class SqlDataSource
{
    /// <summary>
    /// Регистрирует пул соединений, из которого берут соединения репозитории.
    /// </summary>
    public static IServiceCollection AddSqlDataSource(
        this IServiceCollection services,
        string connectionString,
        Action<NpgsqlDataSourceBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddSingleton(_ =>
        {
            var builder = new NpgsqlDataSourceBuilder(connectionString);

            configure?.Invoke(builder);

            return builder.Build();
        });

        services.AddSingleton<System.Data.Common.DbDataSource>(provider =>
            provider.GetRequiredService<NpgsqlDataSource>());

        return services;
    }
}
