using System.ComponentModel.DataAnnotations;

namespace CodeExample.CurrencyService.Client;

/// <summary>
/// Как обратиться к сервису курсов и как в нём аутентифицироваться.
/// </summary>
public sealed class CurrencyClientOptions
{
    /// <summary>
    /// Заголовок, в котором передаётся общий ключ.
    /// </summary>
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";

    /// <summary>Базовый адрес сервиса курсов.</summary>
    [Required(ErrorMessage = "CurrencyClientOptions:BaseUrl is required.")]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Таймаут одной попытки в секундах, отдельно от общего, чтобы повтор успел завершиться.</summary>
    [Range(1, 120, ErrorMessage = "CurrencyClientOptions:TimeoutSeconds must be between 1 and 120.")]
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>Общий ключ, отправляемый в <see cref="ApiKeyHeaderName"/>. Значение то же, что проверяет сервис.</summary>
    [Required(ErrorMessage = "CurrencyClientOptions:ApiKey is required.")]
    [MinLength(16, ErrorMessage = "CurrencyClientOptions:ApiKey must be at least 16 characters long.")]
    public string ApiKey { get; set; } = string.Empty;
}
