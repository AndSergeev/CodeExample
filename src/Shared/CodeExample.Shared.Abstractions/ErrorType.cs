namespace CodeExample.Shared.Abstractions;

/// <summary>
/// Классификация <see cref="Error"/>: по ней слой представления выбирает http статус.
/// </summary>
public enum ErrorType
{
    Validation = 0,

    NotFound = 1,

    Conflict = 2,

    Unauthorized = 3,

    Unavailable = 4,

    Failure = 5
}
