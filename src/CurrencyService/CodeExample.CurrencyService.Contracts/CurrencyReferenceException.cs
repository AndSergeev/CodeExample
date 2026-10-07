namespace CodeExample.CurrencyService.Contracts;

/// <summary>
/// Сервис курсов не смог ответить.
/// </summary>
public sealed class CurrencyReferenceException : Exception
{
    public CurrencyReferenceException(string message)
        : base(message)
    {
    }

    public CurrencyReferenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Статус ответа, если сервис успел ответить.</summary>
    public int? StatusCode { get; init; }

    /// <summary>Тело ответа с ошибкой, если его удалось прочитать.</summary>
    public string? ResponseBody { get; init; }
}
