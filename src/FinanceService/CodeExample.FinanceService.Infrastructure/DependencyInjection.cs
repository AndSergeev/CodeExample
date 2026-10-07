using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CodeExample.CurrencyService.Client;
using CodeExample.FinanceService.Domain.Abstractions;
using CodeExample.FinanceService.Infrastructure.Persistence.Repositories;
using CodeExample.Shared.Database;

namespace CodeExample.FinanceService.Infrastructure;

/// <summary>Внедрение зависимостей.</summary>
public static class DependencyInjection
{
    public const string ConnectionStringName = "FinanceDatabase";

    /// <summary>Регистрирует пул соединений, репозиторий избранного и клиент сервиса курсов.</summary>
    public static IServiceCollection AddFinanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.RequireConnectionString(ConnectionStringName);

        services.AddSqlDataSource(connectionString);
        services.AddScoped<IUserFavoriteCurrencyRepository, UserFavoriteCurrencyRepository>();
        services.AddCurrencyClient(configuration);

        return services;
    }
}
