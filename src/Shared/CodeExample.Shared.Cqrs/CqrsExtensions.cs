using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using CodeExample.Shared.Abstractions;

namespace CodeExample.Shared.Cqrs;

/// <summary>
/// Регистрация CQRS-конвейера, общего для всех сервисов.
/// </summary>
public static class CqrsExtensions
{
    /// <summary>Регистрирует MediatR для сборки вместе с общими behaviour'ами, валидаторами и часами.</summary>
    public static IServiceCollection AddCqrsApplication(
        this IServiceCollection services,
        Assembly applicationAssembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(applicationAssembly);

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(applicationAssembly);

            configuration.AddOpenBehavior(typeof(UnhandledExceptionBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
