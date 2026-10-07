using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeExample.Shared.Web;

/// <summary>
/// Регистрация и чтение конфигурации, которую потребляет класс настроек.
/// </summary>
public static class OptionsExtensions
{
    /// <summary>Связывает секцию, названную по <typeparamref name="TOptions"/>, и валидирует её при старте.</summary>
    public static OptionsBuilder<TOptions> AddOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services
            .AddOptions<TOptions>()
            .Bind(configuration.GetSection(SectionName<TOptions>()))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    /// <summary>Читает ту же секцию там, где контейнера ещё нет.</summary>
    public static TOptions ReadOptions<TOptions>(this IConfiguration configuration)
        where TOptions : class, new()
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.GetSection(SectionName<TOptions>()).Get<TOptions>() ?? new TOptions();
    }

    private static string SectionName<TOptions>() => typeof(TOptions).Name;
}
