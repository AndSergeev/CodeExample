using System.ComponentModel.DataAnnotations;

namespace CodeExample.Gateway.Configuration;

/// <summary>
/// Пределы ограничителя частоты.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>Сколько запросов пропускается за окно.</summary>
    [Range(1, 100000, ErrorMessage = "RateLimitingOptions:PermitLimit must be between 1 and 100000.")]
    public int PermitLimit { get; set; } = 100;

    /// <summary>Длина окна в секундах.</summary>
    [Range(1, 3600, ErrorMessage = "RateLimitingOptions:WindowSeconds must be between 1 and 3600.")]
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Сколько запросов ждёт очереди сверх предела.</summary>
    [Range(0, 100000, ErrorMessage = "RateLimitingOptions:QueueLimit must be between 0 and 100000.")]
    public int QueueLimit { get; set; }
}
