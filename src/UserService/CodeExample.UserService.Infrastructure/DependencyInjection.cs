using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CodeExample.Shared.Database;
using CodeExample.UserService.Domain.Abstractions;
using CodeExample.UserService.Infrastructure.Persistence;
using CodeExample.UserService.Infrastructure.Persistence.Repositories;
using CodeExample.UserService.Infrastructure.Security;

namespace CodeExample.UserService.Infrastructure;

/// <summary>Внедрение зависимостей.</summary>
public static class DependencyInjection
{
    public const string ConnectionStringName = "UserDatabase";

    /// <summary>Регистрирует пул соединений, репозитории и сервисы безопасности.</summary>
    public static IServiceCollection AddUserInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.RequireConnectionString(ConnectionStringName);

        services.AddSqlDataSource(connectionString);

        services.AddScoped<IUnitOfWork>(provider =>
            new SqlUnitOfWork(provider.GetRequiredService<System.Data.Common.DbDataSource>()));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        return services;
    }
}
