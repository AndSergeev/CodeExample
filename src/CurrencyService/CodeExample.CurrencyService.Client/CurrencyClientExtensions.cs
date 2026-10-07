using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CodeExample.CurrencyService.Contracts;
using CodeExample.Shared.Resilience;

namespace CodeExample.CurrencyService.Client;

/// <summary>
/// Регистрация сервиса курсов на стороне потребителя.
/// </summary>
public static class CurrencyClientExtensions
{
    public static string SectionName => nameof(CurrencyClientOptions);

    /// <summary>
    /// Регистрирует <see cref="ICurrencyClient"/> для сервиса курсов.
    /// </summary>
    public static IServiceCollection AddCurrencyClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<CurrencyClientOptions>()
            .Bind(configuration.GetSection(SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var attemptTimeout = ReadAttemptTimeout(configuration);

        services.AddTransient<InternalApiKeyHandler>();

        services
            .AddHttpClient<ICurrencyClient, CurrencyClient>((serviceProvider, httpClient) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<CurrencyClientOptions>>()
                    .Value;

                httpClient.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);

                // HttpClient.Timeout ограничивает всю цепочку вместе с повторами и перекрыл бы
                // их, поэтому таймауты задаёт конвейер устойчивости.
                httpClient.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddHttpMessageHandler<InternalApiKeyHandler>()
            .AddBoundedResilience(attemptTimeout, ResilienceExtensions.TotalFor(attemptTimeout));

        return services;
    }

    // Таймаут попытки нужен конвейеру до сборки контейнера, поэтому он читается и проверяется
    // здесь вручную: ValidateOnStart сработает позже и опоздает к настройке конвейера.
    private static TimeSpan ReadAttemptTimeout(IConfiguration configuration)
    {
        var options = new CurrencyClientOptions();
        configuration.GetSection(SectionName).Bind(options);

        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true))
        {
            throw new OptionsValidationException(
                SectionName,
                typeof(CurrencyClientOptions),
                results.Select(failure => failure.ErrorMessage ?? "The value is not valid."));
        }

        return TimeSpan.FromSeconds(options.TimeoutSeconds);
    }
}
