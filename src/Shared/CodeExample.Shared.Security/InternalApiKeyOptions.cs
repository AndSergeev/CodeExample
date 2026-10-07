using System.ComponentModel.DataAnnotations;

namespace CodeExample.Shared.Security;

/// <summary>
/// Настройки внутренних, межсервисных эндпоинтов: сервис валют не публикуется через API-шлюз, и
/// единственный вход, общий ключ в <see cref="HeaderName"/>.
/// </summary>
public sealed class InternalApiKeyOptions
{
    /// <summary>Заголовок с общим ключом.</summary>
    public const string HeaderName = "X-Internal-Api-Key";

    [Required(ErrorMessage = "InternalApiKeyOptions:ApiKey is required.")]
    [MinLength(16, ErrorMessage = "InternalApiKeyOptions:ApiKey must be at least 16 characters long.")]
    public string ApiKey { get; set; } = string.Empty;
}
