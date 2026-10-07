using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CodeExample.Shared.Cqrs;
using CodeExample.Shared.Security;
using CodeExample.UserService.Application.Abstractions;

namespace CodeExample.UserService.Application;

/// <summary>
/// Внедрение зависимостей.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Регистрирует обработчики, валидаторы и сервисы токенов.</summary>
    public static IServiceCollection AddUserApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddCqrsApplication(Assembly.GetExecutingAssembly());

        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<ITokenIssuer, TokenIssuer>();

        return services;
    }
}
