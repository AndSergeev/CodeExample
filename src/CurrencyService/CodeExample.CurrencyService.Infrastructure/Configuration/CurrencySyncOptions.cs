using System.ComponentModel.DataAnnotations;

namespace CodeExample.CurrencyService.Infrastructure.Configuration;

/// <summary>
/// Расписание и настройки фоновой синхронизации.
/// </summary>
public sealed class CurrencySyncOptions
{
    /// <summary>
    /// Cron-выражение Quartz, вычисляемое в <see cref="TimeZoneId"/>.
    /// </summary>
    [Required(ErrorMessage = "CurrencySyncOptions:Cron is required.")]
    public string Cron { get; set; } = "0 30 11 * * ?";

    /// <summary>
    /// Часовой пояс, в котором вычисляется cron.
    /// </summary>
    [Required(ErrorMessage = "CurrencySyncOptions:TimeZoneId is required.")]
    public string TimeZoneId { get; set; } = "Russian Standard Time";

    /// <summary>Адрес эндпоинта с курсами ЦБ на день.</summary>
    [Required(ErrorMessage = "CurrencySyncOptions:SourceUrl is required.")]
    public string SourceUrl { get; set; } = "http://www.cbr.ru/scripts/XML_daily.asp";

    /// <summary>Таймаут одной попытки в секундах.</summary>
    [Range(1, 300, ErrorMessage = "CurrencySyncOptions:TimeoutSeconds must be between 1 and 300.")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Общее время запроса вместе со всеми повторами, в секундах.
    /// </summary>
    /// <remarks>
    /// Допустимый диапазон зависит от <see cref="TimeoutSeconds"/>: значение ниже худшего
    /// случая означает, что последняя попытка обрывается общим таймаутом, и тогда любой отказ
    /// приходит как истёкшее время. Поэтому нижнюю границу проверяет не атрибут, а конвейер
    /// устойчивости при сборке: он один знает и число попыток, и паузы между ними.
    /// </remarks>
    public int TotalTimeoutSeconds { get; set; } = 180;
}
