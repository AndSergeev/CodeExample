using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CodeExample.CurrencyService.Domain.Abstractions;
using CodeExample.CurrencyService.Infrastructure.Configuration;
using CodeExample.CurrencyService.Infrastructure.ExternalServices;
using CodeExample.CurrencyService.Infrastructure.Persistence;
using CodeExample.CurrencyService.Infrastructure.Persistence.Repositories;
using CodeExample.Shared.Database;
using CodeExample.Shared.Resilience;
using CodeExample.Shared.Web;

namespace CodeExample.CurrencyService.Infrastructure;

/// <summary>Внедрение зависимостей.</summary>
public static class DependencyInjection
{
    public const string ConnectionStringName = "CurrencyDatabase";

    /// <summary>Регистрирует базу, репозитории и клиент ЦБ РФ.</summary>
    public static IServiceCollection AddCurrencyInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.RequireConnectionString(ConnectionStringName);

        services.AddSqlDataSource(connectionString, builder => builder.MapComposite<CurrencyRow>());
        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddCbrCurrencyRateSource(configuration);

        return services;
    }

    private static IServiceCollection AddCbrCurrencyRateSource(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var syncOptions = configuration.ReadOptions<CurrencySyncOptions>();

        var attemptTimeout = TimeSpan.FromSeconds(syncOptions.TimeoutSeconds);
        var totalTimeout = TimeSpan.FromSeconds(syncOptions.TotalTimeoutSeconds);

        services
            .AddHttpClient<ICurrencyRateSource, CbrCurrencyRateSource>((serviceProvider, client) =>
            {
                client.BaseAddress = new Uri(syncOptions.SourceUrl, UriKind.Absolute);

                // Таймауты задаёт конвейер устойчивости: HttpClient.Timeout ограничивает
                // всю цепочку вместе с повторами и перекрыл бы их.
                client.Timeout = Timeout.InfiniteTimeSpan;

                // Эндпоинт ЦБ отвечает в Windows-1251 и отвергает запросы без
                // user agent, поэтому отправляется содержательный.
                client.DefaultRequestHeaders.UserAgent.ParseAdd("CodeExample-CurrencySync/1.0");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/xml,text/xml,*/*");
            })
            .AddBoundedResilience(attemptTimeout, totalTimeout);

        return services;
    }
}
