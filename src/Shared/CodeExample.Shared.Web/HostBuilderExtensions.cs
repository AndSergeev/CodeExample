using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CodeExample.Shared.Web;

/// <summary>Конфигурация и логирование хоста.</summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Добавляет слои конфигурации: appsettings.json, файл окружения, переменные окружения, от
    /// меньшего приоритета к большему.
    /// </summary>
    public static WebApplicationBuilder ConfigureSettings(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Configuration.AddLayeredFiles(builder.Environment.EnvironmentName);

        return builder;
    }

    /// <inheritdoc cref="ConfigureSettings(WebApplicationBuilder)"/>
    public static HostApplicationBuilder ConfigureSettings(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Configuration.AddLayeredFiles(builder.Environment.EnvironmentName);

        return builder;
    }

    /// <summary>Заменяет встроенное логирование на Serilog из той же конфигурации.</summary>
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        return builder;
    }

    /// <inheritdoc cref="ConfigureLogging(WebApplicationBuilder)"/>
    public static HostApplicationBuilder ConfigureLogging(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        return builder;
    }

    private static void AddLayeredFiles(this ConfigurationManager configuration, string environmentName)
    {
        configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();
    }
}
