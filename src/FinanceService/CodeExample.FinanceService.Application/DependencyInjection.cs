using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CodeExample.Shared.Cqrs;

namespace CodeExample.FinanceService.Application;

/// <summary>
/// Внедрение зависимостей.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Регистрирует обработчики и валидаторы.</summary>
    public static IServiceCollection AddFinanceApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddCqrsApplication(Assembly.GetExecutingAssembly());

        return services;
    }
}
