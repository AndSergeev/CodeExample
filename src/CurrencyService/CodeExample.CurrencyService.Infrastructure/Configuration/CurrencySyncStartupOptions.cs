namespace CodeExample.CurrencyService.Infrastructure.Configuration;

/// <summary>
/// Настройки синхронизации во время старта приложения.
/// </summary>
public sealed class CurrencySyncStartupOptions
{
    /// <summary>
    /// Если true, крон джоба выполняется при старте хоста. Сама джоба проверяет, есть ли уже записи.
    /// </summary>
    public bool RunOnStartup { get; set; } = true;
}
